import { Text, type TextProps } from 'react-native';

export type HeadingProps = TextProps & {
  level?: 1 | 2;
};

/**
 * Átomo de título. `level` controla apenas a escala tipográfica — semântica
 * de heading real fica a cargo de quem compõe a tela.
 */
export function Heading({ level = 1, className, ...textProps }: HeadingProps) {
  const sizeClassName = level === 1 ? 'text-3xl font-bold' : 'text-xl font-semibold';

  return (
    <Text
      className={`${sizeClassName} text-text dark:text-dark-text ${className ?? ''}`}
      {...textProps}
    />
  );
}
