import { TextInput, type TextInputProps } from 'react-native';

export type InputProps = TextInputProps;

/**
 * Átomo de campo de texto. Não conhece regra de negócio — apenas estiliza um
 * TextInput padrão e repassa os demais props (value, onChangeText, etc).
 */
export function Input({ className, ...textInputProps }: InputProps) {
  return (
    <TextInput
      className={`rounded-small border border-background-selected bg-background-element px-three py-two text-base text-text dark:border-dark-background-selected dark:bg-dark-background-element dark:text-dark-text ${className ?? ''}`}
      {...textInputProps}
    />
  );
}
