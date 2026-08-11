/**
 * Fonte única de verdade para cores, espaçamento e raio de borda. Arquivo
 * .js puro (não .ts) para que tailwind.config.js consiga fazer require()
 * direto, sem depender de transpilação TS em tempo de build. Consumido
 * também por src/theme/tokens.ts (app RN), que reexporta isso tipado.
 */
module.exports = {
  Colors: {
    light: {
      text: '#000000',
      background: '#ffffff',
      backgroundElement: '#F0F0F3',
      backgroundSelected: '#E0E1E6',
      textSecondary: '#60646C',
      primary: '#208AEF',
    },
    dark: {
      text: '#ffffff',
      background: '#000000',
      backgroundElement: '#212225',
      backgroundSelected: '#2E3135',
      textSecondary: '#B0B4BA',
      primary: '#4DA3F5',
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
