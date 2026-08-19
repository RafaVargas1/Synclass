import { useRouter } from 'expo-router';
import type { ReactNode } from 'react';
import { Pressable, Text, View } from 'react-native';

import { Crown } from '@/components/atoms/Crown';
import { ZigzagDivider } from '@/components/atoms/ZigzagDivider';
import { Fonts, MaxContentWidth } from '@/theme/tokens';

export type TopbarProps = {
  /** Quando presente, mostra seta de voltar + este título em vez da marca. */
  titulo?: string;
  /** Conteúdo à direita (links de navegação, usuário + Sair, etc). */
  children?: ReactNode;
};

/**
 * Organismo: cabeçalho consistente de toda tela (issue de usabilidade —
 * antes desta issue nenhuma tela tinha forma de voltar). Duas variantes:
 * marca (sem `titulo`, usada na Home e no Painel — telas raiz de
 * navegação) ou voltar+título (demais telas).
 */
export function Topbar({ titulo, children }: TopbarProps) {
  return (
    <View className="border-b border-border bg-background dark:border-dark-border dark:bg-dark-background">
      <View
        className="w-full flex-row items-center justify-between self-center px-four py-three"
        style={{ maxWidth: MaxContentWidth }}
      >
        {titulo ? <TituloComVoltar titulo={titulo} /> : <Marca />}
        {children ? <View className="flex-row items-center gap-three">{children}</View> : null}
      </View>
      <ZigzagDivider />
    </View>
  );
}

function Marca() {
  return (
    <View className="flex-row items-center gap-two">
      <Crown degraus={[10, 16, 20]} />
      <Text
        accessibilityRole="header"
        className="text-lg font-bold tracking-widest text-text dark:text-dark-text"
        style={{ fontFamily: Fonts.deco }}
      >
        SYNCLASS
      </Text>
    </View>
  );
}

function TituloComVoltar({ titulo }: { titulo: string }) {
  const router = useRouter();
  const voltar = () => (router.canGoBack() ? router.back() : router.replace('/painel'));

  return (
    <View className="flex-row items-center gap-three">
      <Pressable accessibilityRole="button" accessibilityLabel="Voltar" onPress={voltar} hitSlop={8}>
        <View
          className="border-l-2 border-t-2 border-text dark:border-dark-text"
          style={{ width: 10, height: 10, transform: [{ rotate: '-45deg' }] }}
        />
      </Pressable>
      <Text accessibilityRole="header" className="text-lg font-bold text-text dark:text-dark-text">
        {titulo}
      </Text>
    </View>
  );
}
