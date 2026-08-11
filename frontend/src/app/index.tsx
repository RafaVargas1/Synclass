import { useRouter } from 'expo-router';

import { HomeTemplate } from '@/components/templates/HomeTemplate';

export default function HomeScreen() {
  const router = useRouter();

  return <HomeTemplate onGetStarted={() => router.push('/professor/cadastro')} />;
}
