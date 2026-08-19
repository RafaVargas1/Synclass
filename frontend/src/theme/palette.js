/**
 * Fonte única de verdade para cores, espaçamento e raio de borda. Arquivo
 * .js puro (não .ts) para que tailwind.config.js consiga fazer require()
 * direto, sem depender de transpilação TS em tempo de build. Consumido
 * também por src/theme/tokens.ts (app RN), que reexporta isso tipado.
 */
module.exports = {
  Colors: {
    light: {
      text: '#14161A',
      background: '#F5F6F8',
      backgroundElement: '#E7E9ED',
      backgroundSelected: '#D8DBE1',
      border: '#D3D6DC',
      textSecondary: '#5B616B',
      primary: '#1873BD',
      error: '#DC2626',
    },
    dark: {
      text: '#F5F6F8',
      background: '#121317',
      backgroundElement: '#1D1F24',
      backgroundSelected: '#292C33',
      border: '#34373E',
      textSecondary: '#A6ACB6',
      primary: '#4DA3F5',
      error: '#F87171',
    },
  },
  Spacing: {
    half: 2,
    one: 4,
    two: 8,
    three: 16,
    four: 24,
    five: 32,
    six: 64,
  },
  Radius: {
    small: 6,
    medium: 12,
    large: 20,
  },
};
