import { Pressable, Text, View } from 'react-native';

import { Crown } from '@/components/atoms/Crown';
import { Button } from '@/components/atoms/Button';
import { Paragraph } from '@/components/atoms/Paragraph';
import { Fonts } from '@/theme/tokens';

export type HomeHeroProps = {
  onGetStarted: () => void;
  onLogin: () => void;
};

/**
 * Organismo: seção de apresentação da Home, na direção visual art deco
 * aprovada pelo Rafael (moldura escalonada em vez de sombra/gradiente —
 * ver docs/spec/design-system.md#tipografia para a decisão da fonte de
 * destaque `Fonts.deco`). `onLogin` é a entrada de navegação para o login
 * por código (issue #18) — ação secundária, para quem já tem conta.
 */
export function HomeHero({ onGetStarted, onLogin }: HomeHeroProps) {
  return (
    <View className="w-full max-w-[480px] items-center gap-five">
      <MolduraDaMarca />
      <Paragraph className="max-w-[420px] text-center text-base leading-6">
        Professor organiza horários e frequência dos seus Alunos, Aluno confirma presença e
        acompanha o que deve, tudo em um só lugar.
      </Paragraph>
      <Divisor />
      <View className="w-full gap-three">
        <Button label="Cadastrar como Professor" onPress={onGetStarted} />
        <Pressable accessibilityRole="button" onPress={onLogin} className="items-center py-two">
          <Text className="text-sm font-semibold text-primary dark:text-dark-primary">
            Já tenho conta, entrar
          </Text>
        </Pressable>
      </View>
      <PainelDoAluno />
    </View>
  );
}

function MolduraDaMarca() {
  return (
    <View className="items-center">
      <Crown degraus={[8, 16, 24]} />
      <View className="border border-text bg-background px-six py-five dark:border-dark-text dark:bg-dark-background">
        <View className="absolute inset-1.5 border border-border dark:border-dark-border" />
        <Text
          className="text-center text-5xl tracking-widest text-text dark:text-dark-text"
          style={{ fontFamily: Fonts.deco }}
        >
          SYNCLASS
        </Text>
        <Text className="mt-one text-center text-xs font-semibold uppercase tracking-widest text-text-secondary dark:text-dark-text-secondary">
          Agenda entre Professor e Aluno
        </Text>
      </View>
      <Crown degraus={[8, 16, 24]} invertido />
    </View>
  );
}

function Divisor() {
  return (
    <View className="w-full flex-row items-center gap-three">
      <View className="h-px flex-1 bg-border dark:bg-dark-border" />
      <View
        className="h-2 w-2 bg-primary dark:bg-dark-primary"
        style={{ transform: [{ rotate: '45deg' }] }}
      />
      <View className="h-px flex-1 bg-border dark:bg-dark-border" />
    </View>
  );
}

function PainelDoAluno() {
  return (
    <View className="w-full overflow-hidden border border-border bg-background-element p-four dark:border-dark-border dark:bg-dark-background-element">
      <View
        className="absolute right-0 top-0 border-primary dark:border-dark-primary"
        style={{ borderTopWidth: 28, borderLeftWidth: 28, borderLeftColor: 'transparent' }}
      />
      <Text className="text-xs font-semibold uppercase tracking-widest text-primary dark:text-dark-primary">
        Aluno
      </Text>
      <Paragraph className="mt-one text-sm">
        Aluno entra por convite: peça ao seu Professor o link enviado por WhatsApp.
      </Paragraph>
    </View>
  );
}
