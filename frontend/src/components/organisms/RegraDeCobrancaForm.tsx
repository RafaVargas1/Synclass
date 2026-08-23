import { useState } from 'react';
import { View } from 'react-native';

import { Button } from '@/components/atoms/Button';
import { ErrorMessage } from '@/components/atoms/ErrorMessage';
import { ChipSelector, type ChipSelectorOption } from '@/components/molecules/ChipSelector';
import { FormField } from '@/components/molecules/FormField';
import type {
  BaseDeContagemAula,
  DefinirRegraDeCobrancaInput,
  RegraDeCobranca,
  TipoRegraDeCobranca,
} from '@/lib/api/regraDeCobranca';

export type RegraDeCobrancaFormProps = {
  enviando: boolean;
  erro?: string;
  /**
   * Regra já configurada para a matrícula, se houver — pré-preenche os
   * campos em vez de deixar a tela voltar ao default (`ValorPorAula`
   * vazio) e o Professor sobrescrever a regra vigente sem perceber (achado
   * de dev-review, rodada 1 do PR #31).
   */
  regraExistente?: RegraDeCobranca;
  onSubmit: (input: DefinirRegraDeCobrancaInput) => void;
};

const OpcoesTipo: readonly ChipSelectorOption<TipoRegraDeCobranca>[] = [
  { valor: 'ValorPorAula', rotulo: 'Valor por aula' },
  { valor: 'FixoMensal', rotulo: 'Fixo mensal' },
  { valor: 'FixoPorAula', rotulo: 'Fixo por aula' },
];
const TipoInicial: TipoRegraDeCobranca = 'ValorPorAula';

const OpcoesBaseDeContagem: readonly ChipSelectorOption<BaseDeContagemAula>[] = [
  { valor: 'Agendamento', rotulo: 'Todas as aulas agendadas' },
  { valor: 'PresencaConfirmada', rotulo: 'Só aulas com presença confirmada' },
];
const BaseDeContagemInicial: BaseDeContagemAula = 'Agendamento';

const MensagemValorInvalido = 'Informe um valor maior que zero.';
const MensagemFrequenciaInvalida = 'Informe uma frequência semanal entre 1 e 7.';

/**
 * Organismo: formulário de definição da regra de cobrança de uma matrícula
 * (issue #11). O campo `frequenciaSemanalContratada` só aparece quando
 * `tipo === 'ValorPorAula'` — primeiro caso de campo condicional no
 * frontend, sem padrão prévio para copiar (ver implementation.md). O
 * `ChipSelector` "Como contar as aulas do período?" (issue #186) aparece
 * pra `ValorPorAula`/`FixoPorAula` — as duas regras que efetivamente contam
 * aula (`FixoMensal` não usa `quantidadeDeAulasNoPeriodo`).
 */
export function RegraDeCobrancaForm({ enviando, erro, regraExistente, onSubmit }: RegraDeCobrancaFormProps) {
  const [tipo, setTipo] = useState<TipoRegraDeCobranca>(regraExistente?.tipo ?? TipoInicial);
  const [valor, setValor] = useState(regraExistente ? String(regraExistente.valor) : '');
  const [frequenciaSemanalContratada, setFrequenciaSemanalContratada] = useState(
    regraExistente?.frequenciaSemanalContratada != null ? String(regraExistente.frequenciaSemanalContratada) : '',
  );
  const [baseDeContagemAula, setBaseDeContagemAula] = useState<BaseDeContagemAula>(
    regraExistente?.baseDeContagemAula ?? BaseDeContagemInicial,
  );
  const [erroCliente, setErroCliente] = useState<string | undefined>(undefined);

  function handleSubmit() {
    const resultado = validar(tipo, valor, frequenciaSemanalContratada, baseDeContagemAula);
    if (!resultado.valido) {
      setErroCliente(resultado.mensagem);
      return;
    }

    setErroCliente(undefined);
    onSubmit(resultado.input);
  }

  const contaAula = tipo === 'ValorPorAula' || tipo === 'FixoPorAula';

  return (
    <View className="w-full gap-four">
      <ChipSelector label="Tipo de cobrança" opcoes={OpcoesTipo} valor={tipo} onChange={setTipo} />
      <FormField label="Valor" value={valor} onChangeText={setValor} placeholder="50.00" keyboardType="numeric" />
      {tipo === 'ValorPorAula' ? (
        <FormField
          label="Frequência semanal contratada"
          value={frequenciaSemanalContratada}
          onChangeText={setFrequenciaSemanalContratada}
          placeholder="3"
          keyboardType="numeric"
        />
      ) : null}
      {contaAula ? (
        <ChipSelector
          label="Como contar as aulas do período?"
          opcoes={OpcoesBaseDeContagem}
          valor={baseDeContagemAula}
          onChange={setBaseDeContagemAula}
        />
      ) : null}
      {(erroCliente ?? erro) ? <ErrorMessage>{erroCliente ?? erro}</ErrorMessage> : null}
      <Button
        label={enviando ? 'Salvando...' : 'Salvar regra de cobrança'}
        onPress={handleSubmit}
        disabled={enviando}
      />
    </View>
  );
}

type ResultadoValidacao =
  { valido: true; input: DefinirRegraDeCobrancaInput } | { valido: false; mensagem: string };

function validar(
  tipo: TipoRegraDeCobranca,
  valorTexto: string,
  frequenciaTexto: string,
  baseDeContagemAula: BaseDeContagemAula,
): ResultadoValidacao {
  const valor = Number(valorTexto);
  if (!Number.isFinite(valor) || valor <= 0) {
    return { valido: false, mensagem: MensagemValorInvalido };
  }

  if (tipo === 'FixoMensal') {
    return { valido: true, input: { tipo, valor, frequenciaSemanalContratada: null, baseDeContagemAula: null } };
  }

  if (tipo === 'FixoPorAula') {
    return { valido: true, input: { tipo, valor, frequenciaSemanalContratada: null, baseDeContagemAula } };
  }

  const frequenciaSemanalContratada = Number(frequenciaTexto);
  if (!Number.isInteger(frequenciaSemanalContratada) || frequenciaSemanalContratada < 1 || frequenciaSemanalContratada > 7) {
    return { valido: false, mensagem: MensagemFrequenciaInvalida };
  }

  return { valido: true, input: { tipo, valor, frequenciaSemanalContratada, baseDeContagemAula } };
}
