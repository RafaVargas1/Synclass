import '@/global.css';

import { DarkTheme, DefaultTheme, Stack, ThemeProvider } from 'expo-router';
import { StatusBar } from 'expo-status-bar';
import { useColorScheme } from 'react-native';

import { SessaoProvider } from '@/lib/auth/contexto-sessao';

export default function RootLayout() {
  const colorScheme = useColorScheme();

  return (
    <ThemeProvider value={colorScheme === 'dark' ? DarkTheme : DefaultTheme}>
      <SessaoProvider>
        <Stack screenOptions={{ headerShown: false }} />
        <StatusBar style="auto" />
      </SessaoProvider>
    </ThemeProvider>
  );
}
