import { Text, type TextProps } from 'react-native';

/**
 * Átomo de texto de corpo, usa a cor secundária dos tokens de tema.
 */
export function Paragraph({ className, ...textProps }: TextProps) {
  return (
    <Text
      className={`text-base text-text-secondary dark:text-dark-text-secondary ${className ?? ''}`}
      {...textProps}
    />
  );
}
