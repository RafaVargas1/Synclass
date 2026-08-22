import '@/global.css';

import { DarkTheme, DefaultTheme, Stack, ThemeProvider, usePathname } from 'expo-router';
import { StatusBar } from 'expo-status-bar';
import { useColorScheme, View } from 'react-native';

import { MenuNavegacao } from '@/components/organisms/MenuNavegacao';
import { SessaoProvider, useSessao } from '@/lib/auth/contexto-sessao';
import { NotificacoesProvider } from '@/lib/notificacoes/contexto-notificacoes';
import { useIsTelaLarga } from '@/lib/useIsTelaLarga';

/**
 * Rotas públicas (sem sessão) — o menu é exclusivo das telas pós-login
 * (achado de usabilidade: a coluna lateral aparecia mesmo em telas
 * públicas quando havia um token salvo, ex: usuário caiu na Home antes do
 * redirect de `useRedirecionarComSessao`, ou navegou pra `/login`/
 * `/professor/cadastro` de propósito já autenticado). Gate por rota, não
 * só por token — `Boolean(token)` sozinho não basta. Lista fechada e
 * pequena hoje; toda rota nova de tela AUTENTICADA não precisa entrar
 * aqui (o padrão é mostrar o menu), só rotas públicas novas precisam ser
 * adicionadas.
 */
const ROTAS_PUBLICAS = ['/', '/login', '/login/verificar', '/professor/cadastro', '/aluno'];

function ehRotaPublica(pathname: string): boolean {
  return ROTAS_PUBLICAS.includes(pathname) || pathname.startsWith('/convite/');
}

export default function RootLayout() {
  const colorScheme = useColorScheme();

  return (
    <ThemeProvider value={colorScheme === 'dark' ? DarkTheme : DefaultTheme}>
      <NotificacoesProvider>
        <SessaoProvider>
          <AppShell />
          <StatusBar style="auto" />
        </SessaoProvider>
      </NotificacoesProvider>
    </ThemeProvider>
  );
}

/**
 * Coluna lateral persistente do menu de navegação em viewport larga
 * (issue #161) — só quando há sessão ativa (`token`) E a rota atual não é
 * pública (`ehRotaPublica`, achado desta correção: um token salvo de uma
 * sessão anterior não deveria fazer o menu aparecer em cima da Home/login/
 * cadastro). `useSessao()` funciona aqui porque este componente já está
 * dentro de `SessaoProvider`.
 */
function AppShell() {
  const { token, papeis, papelAtivo, definirPapelAtivo } = useSessao();
  const telaLarga = useIsTelaLarga();
  const pathname = usePathname();
  const mostrarColunaLateral = telaLarga && Boolean(token) && !ehRotaPublica(pathname);

  return (
    <View className="flex-1 flex-row">
      {mostrarColunaLateral ? (
        <View
          testID="coluna-lateral-menu"
          className="border-r border-border bg-background dark:border-dark-border dark:bg-dark-background"
          style={{ width: 260 }}
        >
          <MenuNavegacao papeis={papeis} papelAtivo={papelAtivo} onSelecionarPapel={definirPapelAtivo} />
        </View>
      ) : null}
      <View className="flex-1">
        <Stack screenOptions={{ headerShown: false }} />
      </View>
    </View>
  );
}
