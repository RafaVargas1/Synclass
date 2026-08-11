import { Text, View } from 'react-native';

import { ErrorMessage } from '@/components/atoms/ErrorMessage';
import { Input, type InputProps } from '@/components/atoms/Input';

export type FormFieldProps = InputProps & {
  label: string;
  errorMessage?: string;
};

/**
 * Molécula: label + input + mensagem de erro opcional, conforme o exemplo
 * de docs/spec/architecture.md#frontend-atomic-design.
 */
export function FormField({ label, errorMessage, ...inputProps }: FormFieldProps) {
  return (
    <View className="w-full gap-one">
      <Text className="text-sm font-medium text-text dark:text-dark-text">{label}</Text>
      <Input {...inputProps} />
      {errorMessage ? <ErrorMessage>{errorMessage}</ErrorMessage> : null}
    </View>
  );
}
