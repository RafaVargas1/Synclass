import { useRouter } from 'expo-router';
import { useState } from 'react';

import { type ResultadoAutenticadoGoogle } from '@/components/molecules/BotaoLoginGoogle';
import { HomeTemplate } from '@/components/templates/HomeTemplate';
import { useSessao } from '@/lib/auth/contexto-sessao';

/**
 * Tela raiz (issue #114): o login com Google fica acessível direto aqui —
 * autenticação de um toque não deveria exigir navegar até `/login` só pra
 * aparecer (Hick's Law, `docs/spec/ux-heuristics.md`). Mesma wiring de
 * sessão que `login/index.tsx` já tinha, agora também na Home — inclusive o
 * tratamento de falha ao persistir a sessão no dispositivo (achado de
 * dev-review, PR #118: faltava o mesmo `try/catch` que `login/index.tsx` já
 * tinha pra esse caso).
 */
export default function HomeScreen() {
  const router = useRouter();
  const { definirSessao } = useSessao();
  const [erro, setErro] = useState<string | undefined>(undefined);

  async function handleAutenticadoGoogle({ token, papeis }: ResultadoAutenticadoGoogle) {
    try {
      await definirSessao(token, papeis);
      router.replace('/painel');
    } catch {
      setErro('Não foi possível concluir o login neste dispositivo. Tente novamente.');
    }
  }

  function handleCadastroPendenteGoogle(email: string) {
    router.push({ pathname: '/login', params: { email } });
  }

  return (
    <HomeTemplate
      onAutenticadoGoogle={handleAutenticadoGoogle}
      onCadastroPendenteGoogle={handleCadastroPendenteGoogle}
      onEntrarComoProfessor={() => router.push('/professor/cadastro')}
      onEntrarComoAluno={() => router.push('/aluno')}
      onLogin={() => router.push('/login')}
      erro={erro}
    />
  );
}
