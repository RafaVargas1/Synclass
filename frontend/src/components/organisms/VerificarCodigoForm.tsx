import { Pressable, Text, View } from 'react-native';

import { Button } from '@/components/atoms/Button';
import { ErrorMessage } from '@/components/atoms/ErrorMessage';
import { FormField } from '@/components/molecules/FormField';

export type VerificarCodigoFormProps = {
  codigo: string;
  erro?: string;
  enviando: boolean;
  onChangeCodigo: (codigo: string) => void;
  onSubmit: () => void;
  onReenviar: () => void;
};

/**
 * Organismo: formulário de confirmação do código OTP (issue #18). Inclui a
 * ação de reenviar (Critério de aceite: "código expirado ou incorreto...
 * permite solicitar um novo código"), sem conhecer a Api.
 */
export function VerificarCodigoForm({
  codigo,
  erro,
  enviando,
  onChangeCodigo,
  onSubmit,
  onReenviar,
}: VerificarCodigoFormProps) {
  return (
    <View className="w-full gap-four">
      <FormField
        label="Código"
        value={codigo}
        onChangeText={onChangeCodigo}
        placeholder="000000"
        keyboardType="number-pad"
        maxLength={6}
      />
      {erro ? <ErrorMessage>{erro}</ErrorMessage> : null}
      <Button
        label={enviando ? 'Confirmando...' : 'Confirmar'}
        onPress={onSubmit}
        disabled={enviando}
      />
      <Pressable accessibilityRole="button" onPress={onReenviar} disabled={enviando}>
        <Text className="text-center text-sm text-primary dark:text-dark-primary">
          Reenviar código
        </Text>
      </Pressable>
    </View>
  );
}
