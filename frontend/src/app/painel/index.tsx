import { Link } from 'expo-router';
import { useEffect, useState } from 'react';
import { View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { Heading } from '@/components/atoms/Heading';
import { AlternadorDePapel } from '@/components/organisms/AlternadorDePapel';
import { buscarPerfil } from '@/lib/api/usuarios';
import { useSessao } from '@/lib/auth/contexto-sessao';
import { useRedirecionarSemSessao } from '@/lib/auth/useRedirecionarSemSessao';

type Acao = { label: string; href: string };

/**
 * Ações do Aluno (issue #44): as duas únicas telas do Aluno sem segmento
 * dinâmico na rota — `/aluno/professores/[professorId]/horarios` e
 * `.../minhas-aulas` continuam fora daqui porque `professorId`, nesse caso,
 * identifica o Professor da matrícula do Aluno, não o próprio Aluno
 * logado, e a sessão (`useSessao`) não carrega esse vínculo hoje — ver nota
 * de limitação conhecida no PR.
 */
function acoesAluno(): Acao[] {
  return [
    { label: 'Ver histórico de frequência', href: '/aluno/historico-frequencia' },
    { label: 'Ver valor devido', href: '/aluno/valor-devido' },
  ];
}

/**
 * Ações do Professor (issue #44). `Cadastrar Aluno` não depende de
 * `usuarioId` (a Api deriva o Professor autenticado do token, issue #23).
 * As demais usam `/professor/{professorId}/...` — `professorId` é o mesmo
 * `Usuario.Id` do Professor logado (não existe uma entidade `Professor`
 * separada, ver `ProfessoresController.Cadastrar`), por isso só aparecem
 * depois que `usuarioId` resolve via `GET /usuarios/me`.
 */
function acoesProfessor(usuarioId: string | undefined): Acao[] {
  const acoes: Acao[] = [{ label: 'Cadastrar Aluno', href: '/professor/alunos/cadastro' }];

  if (usuarioId) {
    acoes.push(
      { label: 'Gerenciar horários', href: `/professor/${usuarioId}/horarios` },
      { label: 'Alocar Aluno em horário', href: `/professor/${usuarioId}/alocacoes` },
      { label: 'Convidar Aluno', href: `/professor/${usuarioId}/convites/novo` },
      { label: 'Ver valor devido', href: `/professor/${usuarioId}/valor-devido` },
    );
  }

  return acoes;
}

function acoesDoPapel(papelAtivo: string | undefined, usuarioId: string | undefined): Acao[] {
  if (papelAtivo === 'Professor') {
    return acoesProfessor(usuarioId);
  }
  if (papelAtivo === 'Aluno') {
    return acoesAluno();
  }
  return [];
}

/**
 * Resolve o `usuarioId` do Professor logado via `GET /usuarios/me` (issue
 * #44) — só busca quando o papel ativo é Professor, já que é a única
 * consumidora hoje (ver `acoesProfessor`).
 */
function useUsuarioIdLogado(token: string | null, papelAtivo: string | undefined): string | undefined {
  const [usuarioId, setUsuarioId] = useState<string | undefined>(undefined);

  useEffect(() => {
    if (!token || papelAtivo !== 'Professor') {
      return;
    }
    let cancelado = false;
    void buscarPerfil().then((resultado) => {
      if (!cancelado && resultado.sucesso) {
        setUsuarioId(resultado.usuarioId);
      }
    });
    return () => {
      cancelado = true;
    };
  }, [token, papelAtivo]);

  return usuarioId;
}

function ListaDeAcoes({ acoes }: { acoes: Acao[] }) {
  if (acoes.length === 0) {
    return null;
  }

  return (
    <View className="border border-background-selected dark:border-dark-background-selected">
      {acoes.map((acao, indice) => (
        <ItemDeAcao key={acao.href} acao={acao} ultimo={indice === acoes.length - 1} />
      ))}
    </View>
  );
}

function ItemDeAcao({ acao, ultimo }: { acao: Acao; ultimo: boolean }) {
  const divisor = ultimo ? '' : 'border-b border-background-selected dark:border-dark-background-selected';

  return (
    <Link href={acao.href} className={`px-four py-three text-base font-semibold text-text dark:text-dark-text ${divisor}`}>
      {acao.label}
    </Link>
  );
}

/**
 * Tela pós-login (issue #4): landing após confirmar o código OTP
 * (app/login/verificar.tsx). Alterna o conteúdo conforme o papel ativo
 * quando o usuário acumula mais de um papel. Ações por papel viram
 * navegação real (issue #44) — ver `acoesDoPapel`.
 */
export default function PainelScreen() {
  const { carregando, token, papeis, papelAtivo, definirPapelAtivo } = useSessao();
  useRedirecionarSemSessao(carregando, token);
  const usuarioId = useUsuarioIdLogado(token, papelAtivo);

  if (carregando || !token) {
    return null;
  }

  return (
    <SafeAreaView className="flex-1 bg-background dark:bg-dark-background">
      <View className="flex-1 gap-four px-four py-four">
        <Heading level={1}>Painel</Heading>
        <AlternadorDePapel papeis={papeis} papelAtivo={papelAtivo} onSelecionarPapel={definirPapelAtivo} />
        <ListaDeAcoes acoes={acoesDoPapel(papelAtivo, usuarioId)} />
        <Link href="/perfil" className="text-primary underline dark:text-dark-primary">
          Meu perfil
        </Link>
      </View>
    </SafeAreaView>
  );
}
