import { useRouter } from 'expo-router';
import type { ReactNode } from 'react';
import { Pressable, Text, View } from 'react-native';

import { Marca } from '@/components/atoms/Marca';
import { TituloDaAba } from '@/lib/TituloDaAba';
import { useIsTelaLarga } from '@/lib/useIsTelaLarga';
import { AlvoDeToqueMinimo, Fonts, MaxContentWidth } from '@/theme/tokens';

export type TopbarProps = {
  /** Quando presente, mostra o botão Voltar + este título em vez da marca. */
  titulo?: string;
  /** Título da aba do navegador para quando a tela usa a variante marca
   *  (sem `titulo` visível, ex: Home/Painel) mas ainda precisa de um
   *  título de aba distinto do fallback genérico. Ignorado se `titulo`
   *  estiver presente — nesse caso `titulo` já serve pros dois papéis. */
  tituloDaAba?: string;
  /** Conteúdo à direita (links de navegação, usuário + Sair, etc). */
  children?: ReactNode;
  /** Menu de navegação (#77). Slot opcional, só relevante em viewport
   *  estreita: `MenuNavegacao` renderiza o próprio botão de abrir/fechar
   *  o painel mobile, montado dentro do cabeçalho junto da marca. Em
   *  viewport larga não é usado aqui — a coluna lateral persistente
   *  (`AppShell`, `frontend/src/app/_layout.tsx`, issue #161) monta
   *  `MenuNavegacao` uma única vez fora do `Topbar`. */
  menuNavegacao?: ReactNode;
};

/**
 * Organismo: cabeçalho consistente de toda tela (issue de usabilidade —
 * antes desta issue nenhuma tela tinha forma de voltar). Duas variantes:
 * marca (sem `titulo`, usada na Home e no Painel — telas raiz de
 * navegação) ou voltar+título (demais telas). Quando há `titulo`, o botão
 * Voltar (seta + texto "Voltar", issue #145) fica numa linha própria abaixo
 * da linha do cabeçalho — não mais ao lado do título —, reforçando com o
 * rótulo visível o que antes era só uma seta abstrata, sempre nessa mesma
 * posição independente de viewport (issue #161 — a distinção que existia
 * entre viewport larga/estreita aqui era só sobre onde o menu entrava; o
 * menu de viewport larga migrou para a coluna lateral persistente em
 * `AppShell`, então o Voltar não tem mais motivo pra mudar de lugar).
 * `menuNavegacao` (#77) é um slot opcional só usado em viewport estreita
 * (botão de abrir o menu mobile, à esquerda da marca). A API
 * (`titulo`/`children`) não muda.
 */
export function Topbar({ titulo, tituloDaAba, children, menuNavegacao }: TopbarProps) {
  const telaLarga = useIsTelaLarga();

  return (
    <>
      <TituloDaAba titulo={tituloDaAba ?? titulo ?? 'Synclass'} />
      {/* z-20: React Native Web dá a toda `View` um `z-index: 0` implícito,
          o que faz `position: relative` (default do RN-Web) criar uma
          stacking context em cada nível — sem um z-index explícito aqui, o
          dropdown absoluto do `MenuNavegacao` (z-10, mas só relevante
          DENTRO da própria stacking context do cabeçalho) nunca ganha do
          corpo da tela (irmão do cabeçalho, mesma stacking context "0" do
          pai comum), que pinta por cima por vir depois no DOM (achado de
          dev-review, PR #125/#129: conteúdo do Painel aparecendo através do
          menu mobile aberto). */}
      <View className="z-20 border-b border-border bg-background dark:border-dark-border dark:bg-dark-background">
        <View
          // z-20 aqui também: essa linha é a única que contém o
          // `menuNavegacao` no mobile (painel `position: fixed`, issue #146).
          // Sem elevar esta linha, o `Voltar` (irmão desta View dentro do
          // Topbar, adicionado depois no DOM) pinta por cima do painel fixo —
          // mesma causa raiz já documentada acima, um nível mais superficial:
          // duas Views irmãs sem z-index explícito pintam na ordem do DOM, e
          // a que vem depois (Voltar) venceria mesmo o painel tendo seu
          // próprio z-index (que só vale dentro da stacking context desta
          // linha, não contra uma irmã dela).
          className="z-20 w-full flex-row items-center justify-between self-center px-four py-three"
          style={{ maxWidth: MaxContentWidth }}
        >
          <View className="flex-row items-center gap-three">
            {!telaLarga && menuNavegacao ? menuNavegacao : null}
            {titulo ? <Titulo titulo={titulo} /> : <Logotipo />}
          </View>
          {children ? <View className="flex-row items-center gap-three">{children}</View> : null}
        </View>
        {titulo ? (
          <View className="w-full self-center px-four pb-three" style={{ maxWidth: MaxContentWidth }}>
            <BotaoVoltar />
          </View>
        ) : null}
      </View>
    </>
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

function Titulo({ titulo }: { titulo: string }) {
  return (
    <Text accessibilityRole="header" className="text-lg font-bold text-text dark:text-dark-text">
      {titulo}
    </Text>
  );
}

function BotaoVoltar() {
  const router = useRouter();
  // '/' (Home), não '/painel': Topbar é usado tanto em telas autenticadas
  // quanto públicas (login, cadastro) — sem histórico de navegação (ex:
  // acessou /login direto pela URL), cair em '/painel' sem sessão fazia
  // useRedirecionarSemSessao mandar de volta pro /login imediatamente,
  // travando o usuário num loop (achado do usuário, ver issue #141). '/'
  // é seguro nos dois casos: sem sessão mostra as opções de entrar/
  // cadastrar; com sessão, não quebra nada — só não pula direto pro Painel.
  const voltar = () => (router.canGoBack() ? router.back() : router.replace('/'));

  return (
    <Pressable
      accessibilityRole="button"
      accessibilityLabel="Voltar"
      onPress={voltar}
      className="flex-row items-center gap-two self-start"
      style={AlvoDeToqueMinimo}
    >
      <View
        className="border-l-2 border-t-2 border-text dark:border-dark-text"
        style={{ width: 10, height: 10, transform: [{ rotate: '-45deg' }] }}
      />
      <Text className="text-sm font-semibold text-text dark:text-dark-text">Voltar</Text>
    </Pressable>
  );
}
