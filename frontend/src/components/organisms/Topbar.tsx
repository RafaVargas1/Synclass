import { useRouter } from 'expo-router';
import type { ReactNode } from 'react';
import { Pressable, Text, View } from 'react-native';

import { Marca } from '@/components/atoms/Marca';
import { useTituloDaAba } from '@/lib/useTituloDaAba';
import { Fonts, MaxContentWidth } from '@/theme/tokens';

export type TopbarProps = {
  /** Quando presente, mostra seta de voltar + este título em vez da marca. */
  titulo?: string;
  /** Conteúdo à direita (links de navegação, usuário + Sair, etc). */
  children?: ReactNode;
  /** Menu de navegação (#77). Slot opcional: no mobile o `MenuNavegacao`
   *  renderiza o próprio botão de abrir/fechar; no desktop, o painel lateral.
   *  Fica à esquerda do título/marca, sem substituir `titulo` nem `children`. */
  menuNavegacao?: ReactNode;
};

/**
 * Organismo: cabeçalho consistente de toda tela (issue de usabilidade —
 * antes desta issue nenhuma tela tinha forma de voltar). Duas variantes:
 * marca (sem `titulo`, usada na Home e no Painel — telas raiz de
 * navegação) ou voltar+título (demais telas). `menuNavegacao` (#77) é um
 * slot opcional à esquerda para o menu persistente/por botão do fluxo
 * autenticado, sem alterar a API existente (`titulo`/`children`).
 */
export function Topbar({ titulo, children, menuNavegacao }: TopbarProps) {
  useTituloDaAba(titulo ?? 'Synclass');

  return (
    <View className="border-b border-border bg-background dark:border-dark-border dark:bg-dark-background">
      <View
        className="w-full flex-row items-center justify-between self-center px-four py-three"
        style={{ maxWidth: MaxContentWidth }}
      >
        <View className="flex-row items-center gap-three">
          {menuNavegacao ? menuNavegacao : null}
          {titulo ? <TituloComVoltar titulo={titulo} /> : <Logotipo />}
        </View>
        {children ? <View className="flex-row items-center gap-three">{children}</View> : null}
      </View>
    </View>
  );
}

function Logotipo() {
  return (
    <View className="flex-row items-center gap-two">
      <Marca escala={0.7} />
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
      <Pressable
        accessibilityRole="button"
        accessibilityLabel="Voltar"
        onPress={voltar}
        className="items-center justify-center"
        style={{ minWidth: 44, minHeight: 44 }}
      >
        <View
          className="border-l-2 border-t-2 border-text dark:border-dark-text"
          style={{ width: 12, height: 12, transform: [{ rotate: '-45deg' }] }}
        />
      </Pressable>
      <Text accessibilityRole="header" className="text-lg font-bold text-text dark:text-dark-text">
        {titulo}
      </Text>
    </View>
  );
}
