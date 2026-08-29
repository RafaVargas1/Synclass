import { useRouter } from 'expo-router';

import { HomeTemplate } from '@/components/templates/HomeTemplate';
import { useAutenticadoApple } from '@/lib/auth/useAutenticadoApple';
import { useAutenticadoGoogle } from '@/lib/auth/useAutenticadoGoogle';
import { useRedirecionarComSessao } from '@/lib/auth/useRedirecionarComSessao';
import { useSessao } from '@/lib/auth/contexto-sessao';

/**
 * Tela raiz (issue #114): o login com Google fica acessível direto aqui —
 * autenticação de um toque não deveria exigir navegar até `/login` só pra
 * aparecer (Hick's Law, `docs/spec/ux-heuristics.md`). O login com Apple
 * (issue #212) segue o mesmo padrão, no mesmo lugar. `useAutenticadoGoogle`/
 * `useAutenticadoApple` (compartilhados com `login/index.tsx`) resolvem o
 * desfecho de sucesso — persistir sessão + navegar pra `/painel`, com o
 * mesmo tratamento de falha ao gravar no dispositivo. Título da aba (issue
 * #133) vem do próprio `Topbar` dentro de `HomeTemplate`
 * (`tituloDaAba="Início"`), não daqui — um segundo `TituloDaAba` aqui fora
 * colidiria com o de dentro do `Topbar` (dois efeitos escrevendo em
 * `document.title` na mesma tela).
 *
 * Sessão já ativa (token salvo) pula a Home direto pra `/painel`
 * (`useRedirecionarComSessao`, achado de usabilidade — pedir login de
 * novo com token válido é fricção desnecessária). Enquanto a sessão salva
 * ainda carrega ou já foi encontrada, não renderiza o conteúdo público
 * (evita o flash da tela de login antes do redirect).
 */
export default function HomeScreen() {
  const router = useRouter();
  const { carregando, token } = useSessao();
  useRedirecionarComSessao(carregando, token);
  const { handleAutenticadoGoogle, erroGoogle } = useAutenticadoGoogle();
  const { handleAutenticadoApple, erroApple } = useAutenticadoApple();

  function handleCadastroPendenteGoogle(email: string) {
    router.push({ pathname: '/login', params: { email } });
  }

  function handleCadastroPendenteApple(email: string) {
    router.push({ pathname: '/login', params: { email } });
  }

  if (carregando || token) {
    return null;
  }

  return (
    <HomeTemplate
      onAutenticadoGoogle={handleAutenticadoGoogle}
      onCadastroPendenteGoogle={handleCadastroPendenteGoogle}
      onAutenticadoApple={handleAutenticadoApple}
      onCadastroPendenteApple={handleCadastroPendenteApple}
      onEntrarComoProfessor={() => router.push('/professor/cadastro')}
      onEntrarComoAluno={() => router.push('/aluno')}
      onLogin={() => router.push('/login')}
      erro={erroGoogle ?? erroApple}
    />
  );
}
