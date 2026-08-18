import { useRouter } from 'expo-router';
import { useEffect } from 'react';

/**
 * Guarda de rota mínima (issue #4, extraída para reuso na issue #27):
 * redireciona para /login sem sessão salva — ainda não há middleware de
 * rota no Expo Router. Compartilhada por toda tela que exige sessão (ex:
 * `app/painel/index.tsx`, `app/perfil.tsx`).
 */
export function useRedirecionarSemSessao(carregando: boolean, token: string | null): void {
  const router = useRouter();
  useEffect(() => {
    if (!carregando && !token) {
      router.replace('/login');
    }
  }, [carregando, token, router]);
}
