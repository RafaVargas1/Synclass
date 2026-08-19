import { useState } from 'react';
import { View } from 'react-native';

import { Button } from '@/components/atoms/Button';
import { ErrorMessage } from '@/components/atoms/ErrorMessage';
import { FormField } from '@/components/molecules/FormField';
import { contatoEhValido, mascararContato, MensagemContatoInvalido } from '@/lib/validacaoContato';

export type GerarConviteFormProps = {
  contato: string;
  erro?: string;
  enviando: boolean;
  onChangeContato: (contato: string) => void;
  onSubmit: () => void;
};

/**
 * Organismo: formulário de geração de convite (issue #2) — só o contato do
 * Aluno, sem campo de nome (o nome é preenchido pelo próprio Aluno no
 * aceite, tela `convite/[token]`). Não conhece a Api — apenas emite os
 * callbacks recebidos por prop, mesmo racional de `CadastroProfessorForm`.
 * Valida o formato do contato no cliente antes de emitir `onSubmit` (issue
 * #45), mesmo padrão de `HorarioForm`.
 */
export function GerarConviteForm({
  contato,
  erro,
  enviando,
  onChangeContato,
  onSubmit,
}: GerarConviteFormProps) {
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
        label="Contato do Aluno"
        value={contato}
        onChangeText={(texto) => onChangeContato(mascararContato(texto))}
        placeholder="E-mail ou telefone"
        errorMessage={erroContato}
      />
      {erro ? <ErrorMessage>{erro}</ErrorMessage> : null}
      <Button
        label={enviando ? 'Gerando...' : 'Gerar convite'}
        onPress={handleSubmit}
        disabled={enviando}
      />
    </View>
  );
}
