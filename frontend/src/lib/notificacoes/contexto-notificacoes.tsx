import { createContext, useCallback, useContext, useEffect, useRef, useState, type ReactNode } from 'react';

import { Notificacao, NotificacaoHost, type TipoNotificacao } from '@/components/organisms/Notificacao';

export type NotificarInput = {
  tipo: TipoNotificacao;
  mensagem: string;
  /**
   * Tempo em ms até a notificação fechar sozinha. `undefined` usa o
   * default do `tipo` (`DURACAO_PADRAO_POR_TIPO` abaixo) — `erro` não
   * fecha sozinho por padrão (precisa de dispensa manual: um erro que
   * some antes de ser lido derrota o propósito de mostrá-lo, Nielsen #9).
   * Sempre pode ser fechada manualmente antes do prazo, com qualquer
   * `tipo`/`duracaoMs`.
   */
  duracaoMs?: number;
};

export type NotificacoesContextValue = {
  notificar: (input: NotificarInput) => void;
};

const NotificacoesContext = createContext<NotificacoesContextValue | undefined>(undefined);

const DURACAO_PADRAO_POR_TIPO: Record<TipoNotificacao, number | undefined> = {
  erro: undefined,
  aviso: 5000,
  informacao: 5000,
  sucesso: 5000,
};

type NotificacaoAtiva = { id: number; tipo: TipoNotificacao; mensagem: string };

/**
 * Provider global de notificações (issue solicitada pelo usuário —
 * componente único de erro/aviso/informação/sucesso, cor e duração
 * configuráveis). Só uma notificação ativa por vez: mostrar uma nova
 * substitui a anterior imediatamente, em vez de empilhar (Hick's Law —
 * várias notificações simultâneas competem por atenção sem ganho real
 * aqui, ver docs/spec/ux-heuristics.md). Montado uma única vez em
 * `app/_layout.tsx`, fora do `SessaoProvider` — precisa funcionar mesmo
 * em telas públicas (erro de rede pode acontecer antes do login).
 */
export function NotificacoesProvider({ children }: { children: ReactNode }) {
  const [ativa, setAtiva] = useState<NotificacaoAtiva | undefined>(undefined);
  const proximoId = useRef(0);
  const timeoutRef = useRef<ReturnType<typeof setTimeout> | undefined>(undefined);

  const fechar = useCallback(() => {
    setAtiva(undefined);
    if (timeoutRef.current) {
      clearTimeout(timeoutRef.current);
    }
  }, []);

  const notificar = useCallback(({ tipo, mensagem, duracaoMs }: NotificarInput) => {
    if (timeoutRef.current) {
      clearTimeout(timeoutRef.current);
    }
    const id = (proximoId.current += 1);
    setAtiva({ id, tipo, mensagem });

    const duracaoEfetiva = duracaoMs ?? DURACAO_PADRAO_POR_TIPO[tipo];
    if (duracaoEfetiva) {
      timeoutRef.current = setTimeout(() => {
        setAtiva((atual) => (atual?.id === id ? undefined : atual));
      }, duracaoEfetiva);
    }
  }, []);

  useEffect(() => {
    return () => {
      if (timeoutRef.current) {
        clearTimeout(timeoutRef.current);
      }
    };
  }, []);

  return (
    <NotificacoesContext.Provider value={{ notificar }}>
      {children}
      {ativa ? (
        <NotificacaoHost>
          <Notificacao tipo={ativa.tipo} mensagem={ativa.mensagem} onFechar={fechar} />
        </NotificacaoHost>
      ) : null}
    </NotificacoesContext.Provider>
  );
}

/**
 * `notificar({ tipo, mensagem, duracaoMs? })` — forma preferida de
 * mostrar erro/aviso/informação/sucesso em qualquer tela (ver
 * docs/spec/ux-heuristics.md#feedback-do-sistema). Precisa estar dentro
 * de `NotificacoesProvider` (montado uma vez em `app/_layout.tsx`).
 */
export function useNotificacoes(): NotificacoesContextValue {
  const contexto = useContext(NotificacoesContext);
  if (!contexto) {
    throw new Error('useNotificacoes deve ser usado dentro de um NotificacoesProvider.');
  }
  return contexto;
}
