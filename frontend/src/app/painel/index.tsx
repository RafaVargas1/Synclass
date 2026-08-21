import { Link } from 'expo-router';
import { Pressable, Text, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { Button } from '@/components/atoms/Button';
import { ErrorMessage } from '@/components/atoms/ErrorMessage';
import { Heading } from '@/components/atoms/Heading';
import { AlternadorDePapel } from '@/components/organisms/AlternadorDePapel';
import { TopbarAutenticada } from '@/components/organisms/TopbarAutenticada';
import { useSessao } from '@/lib/auth/contexto-sessao';
import { useRedirecionarSemSessao } from '@/lib/auth/useRedirecionarSemSessao';
import { periodoDoDia, saudacaoPorPeriodo } from '@/lib/periodoDoDia';
import { secoesDoPapel, type Secao } from '@/lib/secoesPorPapel';
import { usePerfilLogado } from '@/lib/usePerfilLogado';
import { MaxContentWidthPainel } from '@/theme/tokens';

const MensagemErroUsuarioId =
  'Não foi possível carregar suas ações de Professor. Tente novamente.';

function ListaDeAcoes({ acoes }: { acoes: Secao[] }) {
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

function ItemDeAcao({ acao }: { acao: Secao }) {
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
 * navegação real (issue #44) — ver `secoesDoPapel`. Issue #77 (não é
 * substituição, é adição — RN explícita do card): o `TopbarAutenticada`
 * acrescenta o `MenuNavegacao` persistente/lateral, mas o corpo aqui
 * continua com os mesmos cards de ação — outro caminho pra chegar às
 * mesmas seções, não uma troca.
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
      <TopbarAutenticada>
        <BotaoSair onPress={sair} />
      </TopbarAutenticada>
      <View
        className="w-full flex-1 self-center gap-five px-four py-five"
        style={{ maxWidth: MaxContentWidthPainel }}
      >
        {nome ? <Saudacao nome={nome} /> : null}
        <AlternadorDePapel papeis={papeis} papelAtivo={papelAtivo} onSelecionarPapel={definirPapelAtivo} />
        {erro ? <ErroAcoesProfessor onTentarNovamente={tentarNovamente} /> : null}
        <ListaDeAcoes acoes={secoesDoPapel(papelAtivo, usuarioId)} />
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
