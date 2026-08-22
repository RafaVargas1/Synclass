import { Text, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { HomeHero } from '@/components/organisms/HomeHero';
import { type ResultadoAutenticadoGoogle } from '@/components/molecules/BotaoLoginGoogle';
import { Topbar } from '@/components/organisms/Topbar';

export type HomeTemplateProps = {
  onAutenticadoGoogle: (resultado: ResultadoAutenticadoGoogle) => void;
  onCadastroPendenteGoogle: (email: string) => void;
  onEntrarComoProfessor: () => void;
  onEntrarComoAluno: () => void;
  onLogin: () => void;
  erro?: string;
};

/**
 * Template: define o layout da tela (sem dados reais) — `Topbar` sem
 * `titulo` mostra a marca (é a tela raiz, não tem pra onde voltar);
 * `tituloDaAba="Início"` (issue #133) dá um título de aba distinto sem
 * mudar essa variante visual. Sem largura máxima o conteúdo esticava a
 * tela inteira no web, com cara de app mobile mal adaptado — daí o
 * `items-center` no container do conteúdo.
 */
export function HomeTemplate({
  onAutenticadoGoogle,
  onCadastroPendenteGoogle,
  onEntrarComoProfessor,
  onEntrarComoAluno,
  onLogin,
  erro,
}: HomeTemplateProps) {
  return (
    <SafeAreaView className="flex-1 bg-background dark:bg-dark-background">
      <Topbar tituloDaAba="Início">
        <Text onPress={onLogin} className="text-sm font-semibold text-primary dark:text-dark-primary">
          Entrar com código ou e-mail
        </Text>
      </Topbar>
      <View className="flex-1 items-center justify-center px-four py-six">
        <HomeHero
          onAutenticadoGoogle={onAutenticadoGoogle}
          onCadastroPendenteGoogle={onCadastroPendenteGoogle}
          onEntrarComoProfessor={onEntrarComoProfessor}
          onEntrarComoAluno={onEntrarComoAluno}
          onLogin={onLogin}
          erro={erro}
        />
      </View>
    </SafeAreaView>
  );
}
