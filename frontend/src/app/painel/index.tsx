import { Link } from 'expo-router';
import { Pressable, Text, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { Button } from '@/components/atoms/Button';
import { Heading } from '@/components/atoms/Heading';
import { TopbarAutenticada } from '@/components/organisms/TopbarAutenticada';
import { useSessao } from '@/lib/auth/contexto-sessao';
import { useRedirecionarSemSessao } from '@/lib/auth/useRedirecionarSemSessao';
import { periodoDoDia, saudacaoPorPeriodo } from '@/lib/periodoDoDia';
import { usePerfilLogado } from '@/lib/usePerfilLogado';
import { MaxContentWidthPainel } from '@/theme/tokens';

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
 * (app/login/verificar.tsx). Issue #77: os cards de ação por papel
 * (`ListaDeAcoes`) e o `AlternadorDePapel` que viviam aqui migraram para o
 * `MenuNavegacao` do `TopbarAutenticada` — agora disponível em toda tela
 * autenticada, não só no Painel. Renderizá-los aqui também duplicaria a
 * mesma navegação na mesma tela (achado de dev-review, PR #107: dois
 * `AlternadorDePapel`/pares de link com o mesmo nome acessível visíveis ao
 * mesmo tempo). O corpo do Painel fica só com saudação e atalho pro
 * perfil — a navegação por seção é responsabilidade do menu.
 */
export default function PainelScreen() {
  const { carregando, token, sair } = useSessao();
  useRedirecionarSemSessao(carregando, token);
  const { nome } = usePerfilLogado(token);

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
        <Link href="/perfil" asChild>
          <Button label="Meu perfil" />
        </Link>
      </View>
    </SafeAreaView>
  );
}

function BotaoSair({ onPress }: { onPress: () => void }) {
  return (
    <Pressable
      accessibilityRole="button"
      onPress={onPress}
      className="items-center justify-center"
      style={{ minWidth: 44, minHeight: 44 }}
    >
      <Text className="text-sm font-semibold text-text dark:text-dark-text">Sair</Text>
    </Pressable>
  );
}
