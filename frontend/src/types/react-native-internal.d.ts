// react-native não expõe tipos públicos pro caminho interno usado por
// useIsTelaLarga.test.ts para mockar useWindowDimensions isoladamente
// (ver `jest.mock` em src/lib/useIsTelaLarga.test.ts).
declare module 'react-native/Libraries/Utilities/useWindowDimensions';
