import { useRouter } from 'expo-router';
import { useState } from 'react';

import { type ResultadoAutenticadoGoogle } from '@/components/molecules/BotaoLoginGoogle';
import { useSessao } from '@/lib/auth/contexto-sessao';

const MensagemErroSessao = 'Não foi possível concluir o login neste dispositivo. Tente novamente.';

/**
 * Compartilha entre Home (`app/index.tsx`) e Login (`app/login/index.tsx`)
 * o que fazer com um login Google bem-sucedido: persistir a sessão e
 * navegar pra `/painel`, com o mesmo tratamento de falha ao gravar no
 * dispositivo (achado de dev-review, PR #118: as duas telas repetiam o
 * mesmo bloco `try { definirSessao + replace } catch { setErro(...) }`).
 */
export function useAutenticadoGoogle() {
  const router = useRouter();
  const { definirSessao } = useSessao();
  const [erro, setErro] = useState<string | undefined>(undefined);

  async function handleAutenticadoGoogle({ token, papeis }: ResultadoAutenticadoGoogle) {
    try {
      await definirSessao(token, papeis);
      router.replace('/painel');
    } catch {
      setErro(MensagemErroSessao);
    }
  }

  return { handleAutenticadoGoogle, erroGoogle: erro };
}
