import { createContext, useContext, useEffect, useState, type ReactNode } from 'react';

import { lerPapeis, lerToken } from '@/lib/auth/sessao';

export type SessaoContextValue = {
  carregando: boolean;
  token: string | null;
  papeis: string[];
  papelAtivo: string | undefined;
  definirPapelAtivo: (papel: string) => void;
};

const SessaoContext = createContext<SessaoContextValue | undefined>(undefined);

/**
 * Provider da sessão (issue #4): carrega token e papéis salvos
 * (`lib/auth/sessao.ts`) ao montar e expõe o papel ativo — por padrão o
 * primeiro papel salvo — para telas que precisam alternar entre Professor e
 * Aluno (`AlternadorDePapel`, `app/painel`).
 */
export function SessaoProvider({ children }: { children: ReactNode }) {
  const [carregando, setCarregando] = useState(true);
  const [token, setToken] = useState<string | null>(null);
  const [papeis, setPapeis] = useState<string[]>([]);
  const [papelAtivo, setPapelAtivo] = useState<string | undefined>(undefined);

  useEffect(() => {
    async function carregarSessaoSalva() {
      const [tokenSalvo, papeisSalvos] = await Promise.all([lerToken(), lerPapeis()]);
      setToken(tokenSalvo);
      setPapeis(papeisSalvos ?? []);
      setPapelAtivo(papeisSalvos?.[0]);
      setCarregando(false);
    }

    void carregarSessaoSalva();
  }, []);

  const valor: SessaoContextValue = {
    carregando,
    token,
    papeis,
    papelAtivo,
    definirPapelAtivo: setPapelAtivo,
  };

  return <SessaoContext.Provider value={valor}>{children}</SessaoContext.Provider>;
}

export function useSessao(): SessaoContextValue {
  const contexto = useContext(SessaoContext);
  if (!contexto) {
    throw new Error('useSessao deve ser usado dentro de um SessaoProvider.');
  }
  return contexto;
}
