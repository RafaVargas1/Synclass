import { useRouter } from 'expo-router';

import { HomeTemplate } from '@/components/templates/HomeTemplate';
import { useAutenticadoGoogle } from '@/lib/auth/useAutenticadoGoogle';

/**
 * Tela raiz (issue #114): o login com Google fica acessível direto aqui —
 * autenticação de um toque não deveria exigir navegar até `/login` só pra
 * aparecer (Hick's Law, `docs/spec/ux-heuristics.md`). `useAutenticadoGoogle`
 * (compartilhado com `login/index.tsx`) resolve o desfecho de sucesso —
 * persistir sessão + navegar pra `/painel`, com o mesmo tratamento de falha
 * ao gravar no dispositivo. Título da aba (issue #133) vem do próprio
 * `Topbar` dentro de `HomeTemplate` (`tituloDaAba="Início"`), não daqui —
 * um segundo `TituloDaAba` aqui fora colidiria com o de dentro do
 * `Topbar` (dois efeitos escrevendo em `document.title` na mesma tela).
 */
export default function HomeScreen() {
  const router = useRouter();
  const { handleAutenticadoGoogle, erroGoogle } = useAutenticadoGoogle();

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
      erro={erroGoogle}
    />
  );
}
