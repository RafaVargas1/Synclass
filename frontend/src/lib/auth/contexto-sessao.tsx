import { createContext, useContext, useEffect, useState, type ReactNode } from 'react';

import { lerPapeis, lerToken, salvarPapeis, salvarToken } from '@/lib/auth/sessao';

export type SessaoContextValue = {
  carregando: boolean;
  token: string | null;
  papeis: string[];
  papelAtivo: string | undefined;
  definirPapelAtivo: (papel: string) => void;
  definirSessao: (token: string, papeis: string[]) => Promise<void>;
};

const SessaoContext = createContext<SessaoContextValue | undefined>(undefined);

async function carregarSessaoSalva(): Promise<{ token: string | null; papeis: string[] }> {
  const [tokenSalvo, papeisSalvos] = await Promise.all([lerToken(), lerPapeis()]);
  return { token: tokenSalvo, papeis: papeisSalvos ?? [] };
}

/**
 * Estado da sessão (issue #4): carrega token e papéis salvos
 * (`lib/auth/sessao.ts`) ao montar e expõe o papel ativo — por padrão o
 * primeiro papel salvo. Separado de `SessaoProvider` só para caber no
 * limite de 20 linhas por função (`code-style.md`).
 *
 * `definirSessao` é a única forma de logar o usuário: persiste em
 * `lib/auth/sessao.ts` e atualiza o estado em memória juntos, para nenhuma
 * tela conseguir gravar a sessão sem que quem lê `useSessao()` saiba na
 * hora — gravar direto em `sessao.ts` deixaria o `token` em memória `null`
 * até o próximo mount, quebrando qualquer guarda de rota que decida com
 * base em `useSessao()` logo após o login.
 */
function useSessaoState(): SessaoContextValue {
  const [carregando, setCarregando] = useState(true);
  const [token, setToken] = useState<string | null>(null);
  const [papeis, setPapeis] = useState<string[]>([]);
  const [papelAtivo, setPapelAtivo] = useState<string | undefined>(undefined);

  useEffect(() => {
    void carregarSessaoSalva().then((sessaoSalva) => {
      setToken(sessaoSalva.token);
      setPapeis(sessaoSalva.papeis);
      setPapelAtivo(sessaoSalva.papeis[0]);
      setCarregando(false);
    });
  }, []);

  async function definirSessao(tokenNovo: string, papeisNovos: string[]) {
    await Promise.all([salvarToken(tokenNovo), salvarPapeis(papeisNovos)]);
    setToken(tokenNovo);
    setPapeis(papeisNovos);
    setPapelAtivo(papeisNovos[0]);
  }

  return { carregando, token, papeis, papelAtivo, definirPapelAtivo: setPapelAtivo, definirSessao };
}

/** Provider da sessão (issue #4) — ver `useSessaoState` para o comportamento. */
export function SessaoProvider({ children }: { children: ReactNode }) {
  const valor = useSessaoState();
  return <SessaoContext.Provider value={valor}>{children}</SessaoContext.Provider>;
}

export function useSessao(): SessaoContextValue {
  const contexto = useContext(SessaoContext);
  if (!contexto) {
    throw new Error('useSessao deve ser usado dentro de um SessaoProvider.');
  }
  return contexto;
}
