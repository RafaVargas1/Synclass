import { Text, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { HomeHero } from '@/components/organisms/HomeHero';
import { Topbar } from '@/components/organisms/Topbar';

export type HomeTemplateProps = {
  onEntrarComoProfessor: () => void;
  onEntrarComoAluno: () => void;
  onLogin: () => void;
};

/**
 * Template: define o layout da tela (sem dados reais) — `Topbar` sem
 * `titulo` mostra a marca (é a tela raiz, não tem pra onde voltar). Sem
 * largura máxima o conteúdo esticava a tela inteira no web, com cara de
 * app mobile mal adaptado — daí o `items-center` no container do conteúdo.
 */
export function HomeTemplate({
  onEntrarComoProfessor,
  onEntrarComoAluno,
  onLogin,
}: HomeTemplateProps) {
  return (
    <SafeAreaView className="flex-1 bg-background dark:bg-dark-background">
      <Topbar>
        <Text onPress={onLogin} className="text-sm font-semibold text-primary dark:text-dark-primary">
          Já tenho conta, entrar
        </Text>
      </Topbar>
      <View className="flex-1 items-center justify-center px-four py-six">
        <HomeHero
          onEntrarComoProfessor={onEntrarComoProfessor}
          onEntrarComoAluno={onEntrarComoAluno}
          onLogin={onLogin}
        />
      </View>
    </SafeAreaView>
  );
}
