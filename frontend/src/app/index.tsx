import { useRouter } from 'expo-router';

import { HomeTemplate } from '@/components/templates/HomeTemplate';

export default function HomeScreen() {
  const router = useRouter();

  return (
    <HomeTemplate
      onEntrarComoProfessor={() => router.push('/professor/cadastro')}
      onEntrarComoAluno={() => router.push('/aluno')}
      onLogin={() => router.push('/login')}
    />
  );
}
