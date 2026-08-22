import '@/global.css';

import { DarkTheme, DefaultTheme, Stack, ThemeProvider } from 'expo-router';
import { StatusBar } from 'expo-status-bar';
import { useColorScheme, View } from 'react-native';

import { MenuNavegacao } from '@/components/organisms/MenuNavegacao';
import { SessaoProvider, useSessao } from '@/lib/auth/contexto-sessao';
import { useIsTelaLarga } from '@/lib/useIsTelaLarga';

export default function RootLayout() {
  const colorScheme = useColorScheme();

  return (
    <ThemeProvider value={colorScheme === 'dark' ? DarkTheme : DefaultTheme}>
      <SessaoProvider>
        <AppShell />
        <StatusBar style="auto" />
      </SessaoProvider>
    </ThemeProvider>
  );
}

/**
 * Coluna lateral persistente do menu de navegação em viewport larga
 * (issue #161) — só quando há sessão ativa (`token`); sem sessão (Home,
 * login, cadastro) o app inteiro continua sem coluna nenhuma, mesmo em
 * tela larga. `useSessao()` funciona aqui porque este componente já está
 * dentro de `SessaoProvider`.
 */
function AppShell() {
  const { token, papeis, papelAtivo, definirPapelAtivo } = useSessao();
  const telaLarga = useIsTelaLarga();
  const mostrarColunaLateral = telaLarga && Boolean(token);

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
