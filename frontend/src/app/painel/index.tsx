import { Link } from 'expo-router';
import { useEffect, useState } from 'react';
import { ActivityIndicator, Pressable, Text, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { ErrorMessage } from '@/components/atoms/ErrorMessage';
import { Heading } from '@/components/atoms/Heading';
import { ResumoValorReceber } from '@/components/organisms/ResumoValorReceber';
import { TopbarAutenticada } from '@/components/organisms/TopbarAutenticada';
import { listarValorDevido, type ListarValorDevidoResultado } from '@/lib/api/valorDevido';
import { useSessao } from '@/lib/auth/contexto-sessao';
import { useRedirecionarSemSessao } from '@/lib/auth/useRedirecionarSemSessao';
import { periodoDoDia, saudacaoPorPeriodo } from '@/lib/periodoDoDia';
import { secoesDoPapel, type Secao } from '@/lib/secoesPorPapel';
import { usePerfilLogado } from '@/lib/usePerfilLogado';
import { AlvoDeToqueMinimo, MaxContentWidthPainel } from '@/theme/tokens';

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
 * (app/login/verificar.tsx). O corpo mostra as ações do papel ativo como
 * cards — o objetivo real do app pro usuário que acabou de entrar é agir
 * (marcar horário, ver frequência, ver quanto vai receber), não navegar até
 * um menu escondido pra descobrir o que dá pra fazer (Nielsen #1,
 * visibilidade do que o sistema oferece). `MenuNavegacao` (na
 * `TopbarAutenticada`) continua disponível em toda tela pra navegar embora
 * daqui, mas o Painel não depende dele pra mostrar as próprias ações —
 * evita a tela ficar vazia com só uma saudação e um botão solto (achado de
 * UX reportado pelo usuário: um CTA de "Meu perfil" do tamanho de ação
 * primária numa tela sem mais nada, quando o próprio menu já tem esse link
 * — ver `SecaoMeuPerfil` em `MenuNavegacao.tsx`).
 */
export default function PainelScreen() {
  const { carregando, token, papelAtivo, sair } = useSessao();
  useRedirecionarSemSessao(carregando, token);
  const { usuarioId, nome } = usePerfilLogado(token);
  const acoes = secoesDoPapel(papelAtivo, usuarioId);

  if (carregando || !token) {
    return null;
  }

  return (
    <SafeAreaView className="flex-1 bg-background dark:bg-dark-background">
      <TopbarAutenticada tituloDaAba="Painel">
        <BotaoSair onPress={sair} />
      </TopbarAutenticada>
      <View
        className="w-full flex-1 self-center gap-five px-four py-five"
        style={{ maxWidth: MaxContentWidthPainel }}
      >
        {nome ? <Saudacao nome={nome} /> : null}
        {papelAtivo === 'Professor' && usuarioId ? (
          <ResumoValorReceberComConsulta professorId={usuarioId} />
        ) : null}
        <View className="w-full flex-row flex-wrap gap-three">
          {acoes.map((acao) => (
            <CardDeAcao key={acao.label} acao={acao} />
          ))}
        </View>
      </View>
    </SafeAreaView>
  );
}

/**
 * Estado de consulta do total a receber do mês no Painel (issue #166).
 * Reaproveita `listarValorDevido(professorId)` sem período — mesma chamada
 * que o filtro "Este mês" da tela de valor devido, então o backend resolve
 * o mês corrente (contrato do filtro "Este mês"). A tela compõe o estado de
 * consulta; `ResumoValorReceber` é só apresentação (mesma separação de
 * `valor-devido.tsx`/`ValorDevidoCard`).
 */
function ResumoValorReceberComConsulta({ professorId }: { professorId: string }) {
  const [resultado, setResultado] = useState<ListarValorDevidoResultado | undefined>(undefined);

  useEffect(() => {
    let cancelado = false;
    listarValorDevido(professorId).then((dados) => {
      if (!cancelado) setResultado(dados);
    });
    return () => {
      cancelado = true;
    };
  }, [professorId]);

  if (!resultado) {
    return <ActivityIndicator accessibilityLabel="Carregando" />;
  }
  if (!resultado.sucesso) {
    return <ErrorMessage>{resultado.mensagem}</ErrorMessage>;
  }
  const total = resultado.valoresDevidos.reduce(
    (soma, item) => (item.valor === null ? soma : soma + item.valor),
    0,
  );
  return <ResumoValorReceber total={total} />;
}

function CardDeAcao({ acao }: { acao: Secao }) {
  return (
    <Link href={acao.href} asChild>
      <Pressable
        accessibilityRole="button"
        className="min-w-[160px] flex-1 items-start justify-center gap-one rounded-medium border border-background-selected bg-background-element px-four py-four active:opacity-80 dark:border-dark-background-selected dark:bg-dark-background-element"
        style={{ minHeight: 72 }}
      >
        <Text className="text-base font-semibold text-text dark:text-dark-text">{acao.label}</Text>
      </Pressable>
    </Link>
  );
}

function BotaoSair({ onPress }: { onPress: () => void }) {
  return (
    <Pressable
      accessibilityRole="button"
      onPress={onPress}
      className="items-center justify-center"
      style={AlvoDeToqueMinimo}
    >
      <Text className="text-sm font-semibold text-text dark:text-dark-text">Sair</Text>
    </Pressable>
  );
}
