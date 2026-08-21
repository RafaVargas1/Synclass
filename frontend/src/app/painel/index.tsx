import { Link, type Href } from 'expo-router';
import { useEffect, useState } from 'react';
import { Pressable, Text, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { Button } from '@/components/atoms/Button';
import { ErrorMessage } from '@/components/atoms/ErrorMessage';
import { Heading } from '@/components/atoms/Heading';
import { AlternadorDePapel } from '@/components/organisms/AlternadorDePapel';
import { Topbar } from '@/components/organisms/Topbar';
import { buscarPerfil } from '@/lib/api/usuarios';
import { useSessao } from '@/lib/auth/contexto-sessao';
import { useRedirecionarSemSessao } from '@/lib/auth/useRedirecionarSemSessao';
import { periodoDoDia, saudacaoPorPeriodo } from '@/lib/periodoDoDia';
import { MaxContentWidthPainel } from '@/theme/tokens';

const MensagemErroUsuarioId =
  'Não foi possível carregar suas ações de Professor. Tente novamente.';

type Acao = { label: string; href: Href };

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
      { label: 'Gerenciar horários', href: `/professor/${usuarioId}/horarios` as Href },
      { label: 'Alocar Aluno em horário', href: `/professor/${usuarioId}/alocacoes` as Href },
      { label: 'Convidar Aluno', href: `/professor/${usuarioId}/convites/novo` as Href },
      { label: 'Ver valor devido', href: `/professor/${usuarioId}/valor-devido` as Href },
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

type EstadoPerfilLogado = {
  usuarioId: string | undefined;
  nome: string | undefined;
  erro: boolean;
  tentarNovamente: () => void;
};

/**
 * Resolve o perfil do usuário logado via `GET /usuarios/me` (issue #44 e
 * #69): qualquer papel com `token` busca, já que `nome` alimenta a saudação
 * do Painel (Professor e Aluno) e `usuarioId` só é relevante pros links de
 * Professor (ver `acoesProfessor`). Para de buscar assim que resolve uma
 * vez (guarda por `usuarioId` já preenchido): sem isso, alternar entre
 * papéis via `AlternadorDePapel` refaria a chamada a cada troca, mesmo o
 * Professor não podendo ter um `usuarioId` diferente na mesma sessão
 * (achado de dev-review, PR #55). Em caso de falha, expõe `erro` e
 * `tentarNovamente` em vez de deixar as ações do Professor sumirem sem
 * explicação nem forma de recuperar (mesmo achado).
 */
function usePerfilLogado(token: string | null): EstadoPerfilLogado {
  const [usuarioId, setUsuarioId] = useState<string | undefined>(undefined);
  const [nome, setNome] = useState<string | undefined>(undefined);
  const [erro, setErro] = useState(false);
  const [tentativa, setTentativa] = useState(0);

  useEffect(() => {
    if (!token || usuarioId) {
      return;
    }
    let cancelado = false;
    void buscarPerfil().then((resultado) => {
      if (cancelado) return;
      if (!resultado.sucesso) {
        setErro(true);
        return;
      }
      setErro(false);
      setUsuarioId(resultado.usuarioId);
      setNome(resultado.nome);
    });
    return () => {
      cancelado = true;
    };
  }, [token, tentativa, usuarioId]);

  return { usuarioId, nome, erro, tentarNovamente: () => setTentativa((atual) => atual + 1) };
}

function ListaDeAcoes({ acoes }: { acoes: Acao[] }) {
  if (acoes.length === 0) {
    return null;
  }

  return (
    <View className="flex-row flex-wrap gap-three">
      {acoes.map((acao) => (
        <ItemDeAcao key={acao.label} acao={acao} />
      ))}
    </View>
  );
}

function ItemDeAcao({ acao }: { acao: Acao }) {
  return (
    <Link
      href={acao.href}
      className="w-full min-w-[220px] flex-1 basis-[45%] border border-border bg-background-element px-four py-four dark:border-dark-border dark:bg-dark-background-element"
    >
      <Text className="text-base font-semibold text-text dark:text-dark-text">{acao.label}</Text>
    </Link>
  );
}

/**
 * Saudação de topo do Painel (issue #69): `"{Saudação}, {nome}"`, com o
 * período derivado da hora atual via `periodoDoDia`. Só renderiza quando o
 * `nome` do perfil resolve via `GET /usuarios/me` — antes disso não há o
 * que cumprimentar.
 */
function Saudacao({ nome }: { nome: string }) {
  const saudacao = saudacaoPorPeriodo[periodoDoDia(new Date().getHours())];
  return <Heading>{`${saudacao}, ${nome}`}</Heading>;
}

/**
 * Tela pós-login (issue #4): landing após confirmar o código OTP
 * (app/login/verificar.tsx). Alterna o conteúdo conforme o papel ativo
 * quando o usuário acumula mais de um papel. Ações por papel viram
 * navegação real (issue #44) — ver `acoesDoPapel`.
 */
export default function PainelScreen() {
  const { carregando, token, papeis, papelAtivo, definirPapelAtivo, sair } = useSessao();
  useRedirecionarSemSessao(carregando, token);
  const { usuarioId, nome, erro, tentarNovamente } = usePerfilLogado(token);

  if (carregando || !token) {
    return null;
  }

  return (
    <SafeAreaView className="flex-1 bg-background dark:bg-dark-background">
      <Topbar>
        <BotaoSair onPress={sair} />
      </Topbar>
      <View
        className="w-full flex-1 self-center gap-five px-four py-five"
        style={{ maxWidth: MaxContentWidthPainel }}
      >
        {nome ? <Saudacao nome={nome} /> : null}
        <AlternadorDePapel papeis={papeis} papelAtivo={papelAtivo} onSelecionarPapel={definirPapelAtivo} />
        {erro ? <ErroAcoesProfessor onTentarNovamente={tentarNovamente} /> : null}
        <ListaDeAcoes acoes={acoesDoPapel(papelAtivo, usuarioId)} />
        <Link href="/perfil" asChild>
          <Button label="Meu perfil" />
        </Link>
      </View>
    </SafeAreaView>
  );
}

function BotaoSair({ onPress }: { onPress: () => void }) {
  return (
    <Pressable accessibilityRole="button" onPress={onPress} hitSlop={8}>
      <Text className="text-sm font-semibold text-text dark:text-dark-text">Sair</Text>
    </Pressable>
  );
}

function ErroAcoesProfessor({ onTentarNovamente }: { onTentarNovamente: () => void }) {
  return (
    <View className="gap-two">
      <ErrorMessage>{MensagemErroUsuarioId}</ErrorMessage>
      <Button label="Tentar novamente" onPress={onTentarNovamente} />
    </View>
  );
}
