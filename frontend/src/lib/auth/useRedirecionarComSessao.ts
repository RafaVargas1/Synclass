import { useRouter } from 'expo-router';
import { useEffect } from 'react';

/**
 * Guarda de rota inversa de `useRedirecionarSemSessao`: redireciona pra
 * `/painel` quando JÁ existe uma sessão salva — evita pedir login de novo
 * (Google/código) pra quem já tem token válido, só porque caiu na Home
 * (achado de usabilidade: fricção desnecessária, priorizar continuar a
 * sessão existente). Usada só na Home (`app/index.tsx`) — as demais telas
 * públicas (login, cadastro) não redirecionam automaticamente, o usuário
 * pode ter chegado ali de propósito (ex: trocar de conta).
 */
export function useRedirecionarComSessao(carregando: boolean, token: string | null): void {
  const router = useRouter();
  useEffect(() => {
    if (!carregando && token) {
      router.replace('/painel');
    }
  }, [carregando, token, router]);
}
