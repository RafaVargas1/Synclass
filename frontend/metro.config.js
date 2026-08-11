const { getDefaultConfig } = require('expo/metro-config');
// Mesmo módulo usado internamente por @expo/metro-config para os bloqueios
// padrão (ver ExpoMetroConfig.js) — caminho estável via subpath exports do
// pacote @expo/metro, ao contrário de metro-config/src/... (não exportado).
const exclusionList = require('@expo/metro/metro-config/defaults/exclusionList').default;
const { withNativeWind } = require('nativewind/metro');

const config = getDefaultConfig(__dirname);

// Expo Router monta as rotas a partir de um require.context sobre src/app
// que inclui todo .tsx por padrão (ver expo-router/_ctx.web.js) — sem esse
// bloqueio, um teste co-localizado como src/app/professor/cadastro.test.tsx
// seria tratado como uma rota própria e quebraria o bundle. Jest não passa
// pelo Metro, então os testes continuam rodando normalmente.
config.resolver.blockList = [
  ...(Array.isArray(config.resolver.blockList) ? config.resolver.blockList : [config.resolver.blockList]),
  exclusionList([/\.test\.[jt]sx?$/, /\.spec\.[jt]sx?$/]),
];

module.exports = withNativeWind(config, { input: './src/global.css' });
