import { View } from 'react-native';

import { Button } from '@/components/atoms/Button';
import { IntroSection } from '@/components/molecules/IntroSection';

export type HomeHeroProps = {
  onGetStarted: () => void;
};

/**
 * Organismo: seção completa de UI, compõe uma molécula e um átomo e pode
 * carregar interação própria (aqui, o callback do botão principal).
 */
export function HomeHero({ onGetStarted }: HomeHeroProps) {
  return (
    <View className="items-center gap-four">
      <IntroSection
        title="Synclass"
        description="Fundação do projeto pronta: Docker, PostgreSQL, .NET e Expo conectados. Os requisitos funcionais ainda serão implementados."
      />
      <Button label="Cadastrar como Professor" onPress={onGetStarted} />
    </View>
  );
}
