import { useState } from 'react';
import { View } from 'react-native';

import { Button } from '@/components/atoms/Button';
import { ErrorMessage } from '@/components/atoms/ErrorMessage';
import { ChipSelector, type ChipSelectorOption } from '@/components/molecules/ChipSelector';
import { FormField } from '@/components/molecules/FormField';
import { TipoMarcacao, type CriarHorarioInput, type Horario } from '@/lib/api/horarios';
import { NomesDiaSemana } from '@/lib/diaSemana';
import { horariosSeSobrepoe } from '@/lib/horarioConflito';

const OpcoesDiaSemana: readonly ChipSelectorOption<number>[] = NomesDiaSemana.map((nome, dia) => ({
  valor: dia,
  rotulo: nome.slice(0, 3),
}));

const OpcoesTipoMarcacao: readonly ChipSelectorOption<TipoMarcacao>[] = [
  { valor: TipoMarcacao.Livre, rotulo: 'Livre' },
  { valor: TipoMarcacao.Fixo, rotulo: 'Fixo' },
  { valor: TipoMarcacao.Hibrido, rotulo: 'Híbrido' },
];

export type HorarioFormProps = {
  horariosExistentes: Horario[];
  enviando: boolean;
  erro?: string;
  onSubmit: (input: CriarHorarioInput) => void;
};

const MensagemFormatoHoraInvalido = 'Informe a hora no formato HH:mm.';
const MensagemDuracaoInvalida = 'Informe uma duração em minutos maior que zero.';
const MensagemLimiteAlunosInvalido = 'Informe um limite de alunos maior que zero.';
const MensagemConflito = 'Esse horário conflita com um já cadastrado.';
const MensagemPoliticaObrigatoria = 'Escolha a política de marcação deste horário.';

/**
 * Organismo: formulário de criação de horário disponível (issue #6). Valida
 * formato/duração e conflito no cliente antes de chamar `onSubmit` —
 * feedback imediato para casos óbvios, sem esperar o round-trip da Api (ver
 * Critérios técnicos da issue #6). A Api continua sendo a fonte de verdade.
 */
export function HorarioForm({ horariosExistentes, enviando, erro, onSubmit }: HorarioFormProps) {
  const [diaSemana, setDiaSemana] = useState(1);
  const [tipoMarcacao, setTipoMarcacao] = useState<TipoMarcacao | undefined>(undefined);
  const [horaInicio, setHoraInicio] = useState('');
  const [duracaoMinutos, setDuracaoMinutos] = useState('');
  const [limiteAlunos, setLimiteAlunos] = useState('1');
  const [erroCliente, setErroCliente] = useState<string | undefined>(undefined);

  function handleSubmit() {
    const resultado = validar(
      diaSemana,
      tipoMarcacao,
      horaInicio,
      duracaoMinutos,
      limiteAlunos,
      horariosExistentes,
    );
    if (!resultado.valido) {
      setErroCliente(resultado.mensagem);
      return;
    }

    setErroCliente(undefined);
    onSubmit(resultado.input);
  }

  return (
    <View className="w-full gap-four">
      <ChipSelector
        label="Dia da semana"
        opcoes={OpcoesDiaSemana}
        valor={diaSemana}
        onChange={setDiaSemana}
      />
      <ChipSelector
        label="Política de marcação"
        opcoes={OpcoesTipoMarcacao}
        valor={tipoMarcacao}
        onChange={setTipoMarcacao}
      />
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
      <FormField
        label="Limite de alunos"
        value={limiteAlunos}
        onChangeText={setLimiteAlunos}
        placeholder="1"
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
  tipoMarcacao: TipoMarcacao | undefined,
  horaInicio: string,
  duracaoMinutos: string,
  limiteAlunos: string,
  horariosExistentes: Horario[],
): ResultadoValidacao {
  if (tipoMarcacao === undefined) {
    return { valido: false, mensagem: MensagemPoliticaObrigatoria };
  }

  if (!/^\d{2}:\d{2}$/.test(horaInicio)) {
    return { valido: false, mensagem: MensagemFormatoHoraInvalido };
  }

  const duracao = Number(duracaoMinutos);
  if (!Number.isInteger(duracao) || duracao < 1) {
    return { valido: false, mensagem: MensagemDuracaoInvalida };
  }

  const limite = Number(limiteAlunos);
  if (!Number.isInteger(limite) || limite < 1) {
    return { valido: false, mensagem: MensagemLimiteAlunosInvalido };
  }

  const input: CriarHorarioInput = {
    diaSemana,
    tipoMarcacao,
    horaInicio: `${horaInicio}:00`,
    duracaoMinutos: duracao,
    limiteAlunos: limite,
  };
  const conflita = horariosExistentes.some((existente) => horariosSeSobrepoe(input, existente));
  return conflita ? { valido: false, mensagem: MensagemConflito } : { valido: true, input };
}
