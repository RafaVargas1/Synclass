import { useRouter } from 'expo-router';
import type { ReactNode } from 'react';
import { Pressable, Text, View } from 'react-native';

import { Marca } from '@/components/atoms/Marca';
import { useIsTelaLarga } from '@/lib/useIsTelaLarga';
import { useTituloDaAba } from '@/lib/useTituloDaAba';
import { AlvoDeToqueMinimo, Fonts, MaxContentWidth } from '@/theme/tokens';

export type TopbarProps = {
  /** Quando presente, mostra seta de voltar + este título em vez da marca. */
  titulo?: string;
  /** Conteúdo à direita (links de navegação, usuário + Sair, etc). */
  children?: ReactNode;
  /** Menu de navegação (#77). Slot opcional: no mobile o `MenuNavegacao`
   *  renderiza o próprio botão de abrir/fechar; no desktop, a faixa de
   *  seções de largura total. Fica dentro do cabeçalho junto da marca no
   *  mobile; no desktop vira uma segunda linha abaixo do cabeçalho, fora
   *  do `flex-row` que contém a `Logotipo` — sem isso a faixa de seções,
   *  mais larga que o cabeçalho, empurraria a marca para longe do canto
   *  superior esquerdo (regressão do PR #125, issue #129). */
  menuNavegacao?: ReactNode;
};

/**
 * Organismo: cabeçalho consistente de toda tela (issue de usabilidade —
 * antes desta issue nenhuma tela tinha forma de voltar). Duas variantes:
 * marca (sem `titulo`, usada na Home e no Painel — telas raiz de
 * navegação) ou voltar+título (demais telas). `menuNavegacao` (#77) é um
 * slot opcional: em viewport estreita fica à esquerda da marca (botão de
 * abrir o menu); em viewport larga vira uma segunda linha de largura total
 * abaixo do cabeçalho, para a faixa de seções não competir no mesmo
 * `flex-row` da marca (issue #129). A API (`titulo`/`children`) não muda.
 */
export function Topbar({ titulo, children, menuNavegacao }: TopbarProps) {
  useTituloDaAba(titulo ?? 'Synclass');
  const telaLarga = useIsTelaLarga();

  return (
    // z-20: React Native Web dá a toda `View` um `z-index: 0` implícito, o
    // que faz `position: relative` (default do RN-Web) criar uma stacking
    // context em cada nível — sem um z-index explícito aqui, o dropdown
    // absoluto do `MenuNavegacao` (z-10, mas só relevante DENTRO da própria
    // stacking context do cabeçalho) nunca ganha do corpo da tela (irmão do
    // cabeçalho, mesma stacking context "0" do pai comum), que pinta por
    // cima por vir depois no DOM (achado de dev-review, PR #125/#129:
    // conteúdo do Painel aparecendo através do menu mobile aberto).
    <View className="z-20 border-b border-border bg-background dark:border-dark-border dark:bg-dark-background">
      <View
        className="w-full flex-row items-center justify-between self-center px-four py-three"
        style={{ maxWidth: MaxContentWidth }}
      >
        <View className="flex-row items-center gap-three">
          {!telaLarga && menuNavegacao ? menuNavegacao : null}
          {titulo ? <TituloComVoltar titulo={titulo} /> : <Logotipo />}
        </View>
        {children ? <View className="flex-row items-center gap-three">{children}</View> : null}
      </View>
      {telaLarga && menuNavegacao ? (
        <View
          className="w-full self-center"
          style={{ maxWidth: MaxContentWidth }}
        >
          {menuNavegacao}
        </View>
      ) : null}
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
        style={AlvoDeToqueMinimo}
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
