import { useState } from 'react';
import { Pressable, Text, View } from 'react-native';

import { Button } from '@/components/atoms/Button';
import { ErrorMessage } from '@/components/atoms/ErrorMessage';
import { FormField } from '@/components/molecules/FormField';
import type { CriarHorarioInput, Horario } from '@/lib/api/horarios';
import { NomesDiaSemana } from '@/lib/diaSemana';
import { horariosSeSobrepoe } from '@/lib/horarioConflito';

export type HorarioFormProps = {
  horariosExistentes: Horario[];
  enviando: boolean;
  erro?: string;
  onSubmit: (input: CriarHorarioInput) => void;
};

const MensagemFormatoHoraInvalido = 'Informe a hora no formato HH:mm.';
const MensagemDuracaoInvalida = 'Informe uma duração em minutos maior que zero.';
const MensagemConflito = 'Esse horário conflita com um já cadastrado.';

/**
 * Organismo: formulário de criação de horário disponível (issue #6). Valida
 * formato/duração e conflito no cliente antes de chamar `onSubmit` —
 * feedback imediato para casos óbvios, sem esperar o round-trip da Api (ver
 * Critérios técnicos da issue #6). A Api continua sendo a fonte de verdade.
 */
export function HorarioForm({ horariosExistentes, enviando, erro, onSubmit }: HorarioFormProps) {
  const [diaSemana, setDiaSemana] = useState(1);
  const [horaInicio, setHoraInicio] = useState('');
  const [duracaoMinutos, setDuracaoMinutos] = useState('');
  const [erroCliente, setErroCliente] = useState<string | undefined>(undefined);

  function handleSubmit() {
    const resultado = validar(diaSemana, horaInicio, duracaoMinutos, horariosExistentes);
    if (!resultado.valido) {
      setErroCliente(resultado.mensagem);
      return;
    }

    setErroCliente(undefined);
    onSubmit(resultado.input);
  }

  return (
    <View className="w-full gap-four">
      <DiaSemanaPicker value={diaSemana} onChange={setDiaSemana} />
      <FormField
        label="Hora de início"
        value={horaInicio}
        onChangeText={setHoraInicio}
        placeholder="HH:mm"
      />
      <FormField
        label="Duração (minutos)"
        value={duracaoMinutos}
        onChangeText={setDuracaoMinutos}
        placeholder="60"
        keyboardType="numeric"
      />
      {(erroCliente ?? erro) ? <ErrorMessage>{erroCliente ?? erro}</ErrorMessage> : null}
      <Button
        label={enviando ? 'Salvando...' : 'Adicionar horário'}
        onPress={handleSubmit}
        disabled={enviando}
      />
    </View>
  );
}

type ResultadoValidacao =
  { valido: true; input: CriarHorarioInput } | { valido: false; mensagem: string };

function validar(
  diaSemana: number,
  horaInicio: string,
  duracaoMinutos: string,
  horariosExistentes: Horario[],
): ResultadoValidacao {
  if (!/^\d{2}:\d{2}$/.test(horaInicio)) {
    return { valido: false, mensagem: MensagemFormatoHoraInvalido };
  }

  const duracao = Number(duracaoMinutos);
  if (!Number.isInteger(duracao) || duracao < 1) {
    return { valido: false, mensagem: MensagemDuracaoInvalida };
  }

  const input: CriarHorarioInput = {
    diaSemana,
    horaInicio: `${horaInicio}:00`,
    duracaoMinutos: duracao,
  };
  const conflita = horariosExistentes.some((existente) => horariosSeSobrepoe(input, existente));
  return conflita ? { valido: false, mensagem: MensagemConflito } : { valido: true, input };
}

function DiaSemanaPicker({ value, onChange }: { value: number; onChange: (dia: number) => void }) {
  return (
    <View className="w-full gap-one">
      <Text className="text-sm font-medium text-text dark:text-dark-text">Dia da semana</Text>
      <View className="flex-row flex-wrap gap-one">
        {NomesDiaSemana.map((nome, dia) => (
          <DiaSemanaChip
            key={nome}
            nome={nome}
            selecionado={dia === value}
            onPress={() => onChange(dia)}
          />
        ))}
      </View>
    </View>
  );
}

function DiaSemanaChip({
  nome,
  selecionado,
  onPress,
}: {
  nome: string;
  selecionado: boolean;
  onPress: () => void;
}) {
  const corDeFundo = selecionado
    ? 'border-primary bg-primary dark:border-dark-primary dark:bg-dark-primary'
    : 'border-background-selected bg-background-element dark:border-dark-background-selected dark:bg-dark-background-element';
  const corDoTexto = selecionado ? 'text-white' : 'text-text dark:text-dark-text';

  return (
    <Pressable
      accessibilityRole="button"
      accessibilityState={{ selected: selecionado }}
      onPress={onPress}
      className={`rounded-small border px-two py-one ${corDeFundo}`}
    >
      <Text className={corDoTexto}>{nome.slice(0, 3)}</Text>
    </Pressable>
  );
}
