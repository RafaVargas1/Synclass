import { useRouter } from 'expo-router';

import { type ResultadoAutenticadoGoogle } from '@/components/molecules/BotaoLoginGoogle';
import { HomeTemplate } from '@/components/templates/HomeTemplate';
import { useSessao } from '@/lib/auth/contexto-sessao';

/**
 * Tela raiz (issue #114): o login com Google fica acessível direto aqui —
 * autenticação de um toque não deveria exigir navegar até `/login` só pra
 * aparecer (Hick's Law, `docs/spec/ux-heuristics.md`). Mesma wiring de
 * sessão que `login/index.tsx` já tinha, agora também na Home.
 */
export default function HomeScreen() {
  const router = useRouter();
  const { definirSessao } = useSessao();

  async function handleAutenticadoGoogle({ token, papeis }: ResultadoAutenticadoGoogle) {
    await definirSessao(token, papeis);
    router.replace('/painel');
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
    />
  );
}
