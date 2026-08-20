/**
 * Utilitários de contraste WCAG 2.x (luminância relativa + razão de
 * contraste) para validar tokens de cor da paleta. Util puro, sem
 * dependência de terceiro — usado pelos testes de token (palette.test.ts).
 */

type Canal = 0 | 1 | 2;

/** Converte um canal RGB (0..255) no componente linear usado na fórmula WCAG. */
function componenteLinear(canal: number): number {
  const s = canal / 255;
  return s <= 0.03928 ? s / 12.92 : Math.pow((s + 0.055) / 1.055, 2.4);
}

/** Converte um hex (#RRGGBB) em um array com os três componentes [0..255]. */
function rgbDeHex(hex: string): [number, number, number] {
  const valor = hex.replace('#', '');
  return [0, 2, 4].map((i) => parseInt(valor.slice(i, i + 2), 16)) as [
    number,
    number,
    number,
  ];
}

/**
 * Luminância relativa de uma cor hex no espaço WCAG (0 = preto, 1 = branco).
 * Exemplo: luminanciaRelativa('#FFFFFF') === 1.
 */
export function luminanciaRelativa(hex: string): number {
  const [r, g, b] = rgbDeHex(hex);
  return (
    0.2126 * componenteLinear(r) +
    0.7152 * componenteLinear(g) +
    0.0722 * componenteLinear(b)
  );
}

/**
 * Razão de contraste WCAG entre duas cores hex, sempre >= 1 (L1 >= L2).
 * Exemplo: razaoDeContraste('#000000', '#FFFFFF') === 21.
 */
export function razaoDeContraste(hexA: string, hexB: string): number {
  const la = luminanciaRelativa(hexA);
  const lb = luminanciaRelativa(hexB);
  const maisClara = Math.max(la, lb);
  const maisEscura = Math.min(la, lb);
  return (maisClara + 0.05) / (maisEscura + 0.05);
}
