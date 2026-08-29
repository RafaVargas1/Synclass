import { useRouter } from 'expo-router';
import { useState } from 'react';

import { type ResultadoAutenticadoApple } from '@/components/molecules/BotaoLoginApple';
import { useSessao } from '@/lib/auth/contexto-sessao';

const MensagemErroSessao = 'Não foi possível concluir o login neste dispositivo. Tente novamente.';

/**
 * Compartilha entre Home (`app/index.tsx`), Login (`app/login/index.tsx`) e
 * as telas de cadastro (Professor/Aluno) o que fazer com um login Apple
 * bem-sucedido: persistir a sessão e navegar pra `/painel`, com o mesmo
 * tratamento de falha ao gravar no dispositivo. Espelha
 * `useAutenticadoGoogle` (mesma orquestração, provedor diferente).
 */
export function useAutenticadoApple() {
  const router = useRouter();
  const { definirSessao } = useSessao();
  const [erro, setErro] = useState<string | undefined>(undefined);

  async function handleAutenticadoApple({ token, papeis }: ResultadoAutenticadoApple) {
    try {
      await definirSessao(token, papeis);
      router.replace('/painel');
    } catch {
      setErro(MensagemErroSessao);
    }
  }

  return { handleAutenticadoApple, erroApple: erro };
}
