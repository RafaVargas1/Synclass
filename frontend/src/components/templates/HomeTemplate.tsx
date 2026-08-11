import { View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { HomeHero } from '@/components/organisms/HomeHero';

export type HomeTemplateProps = {
  onGetStarted: () => void;
};

/**
 * Template: define o layout da tela (sem dados reais) — aqui, apenas
 * centraliza o organismo HomeHero na área segura da tela.
 */
export function HomeTemplate({ onGetStarted }: HomeTemplateProps) {
  return (
    <SafeAreaView className="flex-1 bg-background dark:bg-dark-background">
      <View className="flex-1 items-center justify-center px-four">
        <HomeHero onGetStarted={onGetStarted} />
      </View>
    </SafeAreaView>
  );
}
