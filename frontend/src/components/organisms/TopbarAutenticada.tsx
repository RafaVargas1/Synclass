import type { ReactNode } from 'react';

import { MenuNavegacao } from '@/components/organisms/MenuNavegacao';
import { Topbar } from '@/components/organisms/Topbar';
import { useSessao } from '@/lib/auth/contexto-sessao';

export type TopbarAutenticadaProps = {
  /** Quando presente, mostra seta de voltar + este título em vez da marca. */
  titulo?: string;
  /** Conteúdo à direita (Sair, ações da tela). Passado direto ao Topbar. */
  children?: ReactNode;
};

/**
 * Wrapper do fluxo autenticado (#77): pré-integra o `MenuNavegacao` no slot
 * `menuNavegacao` do `Topbar`, resolvendo `papeis`/`papelAtivo`/
 * `definirPapelAtivo` de `useSessao()` uma única vez — em vez de cada tela
 * autenticada resolver a sessão e montar o menu por conta própria. As
 * demais props (`titulo`, `children`) passam direto ao `Topbar`, então
 * trocar `<Topbar>` por `<TopbarAutenticada>` nas telas do fluxo não muda
 * o cabeçalho atual além de acrescentar o menu.
 */
export function TopbarAutenticada({ titulo, children }: TopbarAutenticadaProps) {
  const { papeis, papelAtivo, definirPapelAtivo } = useSessao();

  return (
    <Topbar
      titulo={titulo}
      menuNavegacao={
        <MenuNavegacao papeis={papeis} papelAtivo={papelAtivo} onSelecionarPapel={definirPapelAtivo} />
      }
    >
      {children}
    </Topbar>
  );
}
