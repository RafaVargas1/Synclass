import type { ReactNode } from 'react';
import { Pressable, Text } from 'react-native';

import { MenuNavegacao } from '@/components/organisms/MenuNavegacao';
import { Topbar } from '@/components/organisms/Topbar';
import { useSessao } from '@/lib/auth/contexto-sessao';
import { AlvoDeToqueMinimo } from '@/theme/tokens';

export type TopbarAutenticadaProps = {
  /** Quando presente, mostra seta de voltar + este título em vez da marca. */
  titulo?: string;
  /** Título da aba do navegador para quando a tela usa a variante marca
   *  (sem `titulo`, ex: Painel) mas ainda precisa de um título de aba
   *  distinto do fallback genérico. Ignorado se `titulo` estiver presente
   *  (issue #133). */
  tituloDaAba?: string;
  /** Conteúdo à direita adicional, além do Sair (ações específicas da
   *  tela). Passado direto ao Topbar, depois do botão Sair. */
  children?: ReactNode;
};

/**
 * Wrapper do fluxo autenticado (#77): pré-integra o `MenuNavegacao` no slot
 * `menuNavegacao` do `Topbar`, resolvendo `papeis`/`papelAtivo`/
 * `definirPapelAtivo` de `useSessao()` uma única vez — em vez de cada tela
 * autenticada resolver a sessão e montar o menu por conta própria. Também
 * renderiza o botão "Sair" (antes só existia no Painel) — toda tela
 * autenticada precisa da mesma saída, não só a raiz. As demais props
 * (`titulo`, `tituloDaAba`, `children`) passam direto ao `Topbar`.
 */
export function TopbarAutenticada({ titulo, tituloDaAba, children }: TopbarAutenticadaProps) {
  const { papeis, papelAtivo, definirPapelAtivo, sair } = useSessao();

  return (
    <Topbar
      titulo={titulo}
      tituloDaAba={tituloDaAba}
      menuNavegacao={
        <MenuNavegacao papeis={papeis} papelAtivo={papelAtivo} onSelecionarPapel={definirPapelAtivo} />
      }
    >
      <BotaoSair onPress={sair} />
      {children}
    </Topbar>
  );
}

function BotaoSair({ onPress }: { onPress: () => void }) {
  return (
    <Pressable
      accessibilityRole="button"
      onPress={onPress}
      className="items-center justify-center"
      style={AlvoDeToqueMinimo}
    >
      <Text className="text-sm font-semibold text-text dark:text-dark-text">Sair</Text>
    </Pressable>
  );
}
