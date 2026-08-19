import { Pressable, Text, View } from 'react-native';

import { Button } from '@/components/atoms/Button';
import { IntroSection } from '@/components/molecules/IntroSection';

export type HomeHeroProps = {
  onGetStarted: () => void;
  onLogin: () => void;
};

/**
 * Organismo: seção completa de UI, compõe uma molécula e um átomo e pode
 * carregar interação própria. `onLogin` é a entrada de navegação para o
 * login por código (issue #18) — ação secundária, para quem já tem conta.
 */
export function HomeHero({ onGetStarted, onLogin }: HomeHeroProps) {
  return (
    <View className="items-center gap-four">
      <IntroSection
        title="Synclass"
        description="Professor organiza horários e frequência dos seus Alunos, Aluno confirma presença e acompanha o que deve, tudo em um só lugar."
      />
      <Button label="Cadastrar como Professor" onPress={onGetStarted} />
      <Pressable accessibilityRole="button" onPress={onLogin}>
        <Text className="text-sm text-primary dark:text-dark-primary">
          Já tenho conta, entrar
        </Text>
      </Pressable>
    </View>
  );
}
