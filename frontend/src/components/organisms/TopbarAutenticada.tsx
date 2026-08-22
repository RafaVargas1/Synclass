import type { ReactNode } from 'react';

import { MenuNavegacao } from '@/components/organisms/MenuNavegacao';
import { Topbar } from '@/components/organisms/Topbar';
import { useSessao } from '@/lib/auth/contexto-sessao';

export type TopbarAutenticadaProps = {
  /** Quando presente, mostra seta de voltar + este título em vez da marca. */
  titulo?: string;
  /** Título da aba do navegador para quando a tela usa a variante marca
   *  (sem `titulo`, ex: Painel) mas ainda precisa de um título de aba
   *  distinto do fallback genérico. Ignorado se `titulo` estiver presente
   *  (issue #133). */
  tituloDaAba?: string;
  /** Conteúdo à direita (Sair, ações da tela). Passado direto ao Topbar. */
  children?: ReactNode;
};

/**
 * Wrapper do fluxo autenticado (#77): pré-integra o `MenuNavegacao` no slot
 * `menuNavegacao` do `Topbar`, resolvendo `papeis`/`papelAtivo`/
 * `definirPapelAtivo` de `useSessao()` uma única vez — em vez de cada tela
 * autenticada resolver a sessão e montar o menu por conta própria. As
 * demais props (`titulo`, `tituloDaAba`, `children`) passam direto ao
 * `Topbar`, então trocar `<Topbar>` por `<TopbarAutenticada>` nas telas do
 * fluxo não muda o cabeçalho atual além de acrescentar o menu.
 */
export function TopbarAutenticada({ titulo, tituloDaAba, children }: TopbarAutenticadaProps) {
  const { papeis, papelAtivo, definirPapelAtivo } = useSessao();

  return (
    <Topbar
      titulo={titulo}
      tituloDaAba={tituloDaAba}
      menuNavegacao={
        <MenuNavegacao papeis={papeis} papelAtivo={papelAtivo} onSelecionarPapel={definirPapelAtivo} />
      }
    >
      {children}
    </Topbar>
  );
}
