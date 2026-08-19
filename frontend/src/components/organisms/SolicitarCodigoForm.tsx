import { useState } from 'react';
import { View } from 'react-native';

import { Button } from '@/components/atoms/Button';
import { ErrorMessage } from '@/components/atoms/ErrorMessage';
import { FormField } from '@/components/molecules/FormField';
import { contatoEhValido, mascararContato, MensagemContatoInvalido } from '@/lib/validacaoContato';

export type SolicitarCodigoFormProps = {
  contato: string;
  erro?: string;
  enviando: boolean;
  onChangeContato: (contato: string) => void;
  onSubmit: () => void;
};

/**
 * Organismo: formulário de solicitação de login por código (issue #18). Não
 * conhece a Api — mesmo padrão de CadastroProfessorForm, só emite os
 * callbacks recebidos por prop. Valida o formato do contato no cliente antes
 * de emitir `onSubmit` (issue #45), mesmo padrão de `HorarioForm`.
 */
export function SolicitarCodigoForm({
  contato,
  erro,
  enviando,
  onChangeContato,
  onSubmit,
}: SolicitarCodigoFormProps) {
  const [erroContato, setErroContato] = useState<string | undefined>(undefined);

  function handleSubmit() {
    if (!contatoEhValido(contato)) {
      setErroContato(MensagemContatoInvalido);
      return;
    }
    setErroContato(undefined);
    onSubmit();
  }

  return (
    <View className="w-full gap-four">
      <FormField
        label="Contato"
        value={contato}
        onChangeText={(texto) => onChangeContato(mascararContato(texto, contato))}
        placeholder="E-mail ou telefone cadastrado"
        autoCapitalize="none"
        errorMessage={erroContato}
      />
      {erro ? <ErrorMessage>{erro}</ErrorMessage> : null}
      <Button
        label={enviando ? 'Enviando...' : 'Enviar código'}
        onPress={handleSubmit}
        disabled={enviando}
      />
    </View>
  );
}
