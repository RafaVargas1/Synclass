import { fireEvent, render, screen } from '@testing-library/react-native';

import { RegraDeCobrancaForm } from './RegraDeCobrancaForm';

describe('RegraDeCobrancaForm', () => {
  it('defaults to ValorPorAula and shows the frequencia field', async () => {
    await render(<RegraDeCobrancaForm enviando={false} onSubmit={jest.fn()} />);

    expect(screen.getByText('Frequência semanal contratada')).toBeTruthy();
  });

  it('hides the frequencia field when tipo is FixoMensal', async () => {
    await render(<RegraDeCobrancaForm enviando={false} onSubmit={jest.fn()} />);

    await fireEvent.press(screen.getByText('Fixo mensal'));

    expect(screen.queryByText('Frequência semanal contratada')).toBeNull();
  });

  it('submits ValorPorAula with valor and frequenciaSemanalContratada informed', async () => {
    const onSubmit = jest.fn();
    await render(<RegraDeCobrancaForm enviando={false} onSubmit={onSubmit} />);

    await fireEvent.changeText(screen.getByPlaceholderText('50.00'), '50');
    await fireEvent.changeText(screen.getByPlaceholderText('3'), '3');
    await fireEvent.press(screen.getByText('Salvar regra de cobrança'));

    expect(onSubmit).toHaveBeenCalledWith({
      tipo: 'ValorPorAula',
      valor: 50,
      frequenciaSemanalContratada: 3,
      baseDeContagemAula: 'Agendamento',
    });
  });

  it('submits FixoMensal with frequenciaSemanalContratada and baseDeContagemAula null', async () => {
    const onSubmit = jest.fn();
    await render(<RegraDeCobrancaForm enviando={false} onSubmit={onSubmit} />);

    await fireEvent.press(screen.getByText('Fixo mensal'));
    await fireEvent.changeText(screen.getByPlaceholderText('50.00'), '300');
    await fireEvent.press(screen.getByText('Salvar regra de cobrança'));

    expect(onSubmit).toHaveBeenCalledWith({
      tipo: 'FixoMensal',
      valor: 300,
      frequenciaSemanalContratada: null,
      baseDeContagemAula: null,
    });
  });

  it('shows a client error and does not submit when valor is invalid', async () => {
    const onSubmit = jest.fn();
    await render(<RegraDeCobrancaForm enviando={false} onSubmit={onSubmit} />);

    await fireEvent.press(screen.getByText('Fixo mensal'));
    await fireEvent.press(screen.getByText('Salvar regra de cobrança'));

    expect(onSubmit).not.toHaveBeenCalled();
    expect(screen.getByText('Informe um valor maior que zero.')).toBeTruthy();
  });

  it('shows the Api error message passed via prop', async () => {
    await render(
      <RegraDeCobrancaForm enviando={false} erro="Não foi possível concluir a operação." onSubmit={jest.fn()} />,
    );

    expect(screen.getByText('Não foi possível concluir a operação.')).toBeTruthy();
  });

  it('disables the button and shows Salvando... while enviando', async () => {
    const onSubmit = jest.fn();
    await render(<RegraDeCobrancaForm enviando={true} onSubmit={onSubmit} />);

    await fireEvent.press(screen.getByText('Salvando...'));

    expect(onSubmit).not.toHaveBeenCalled();
  });

  it('pre-fills the fields from regraExistente instead of the ValorPorAula default', async () => {
    const onSubmit = jest.fn();
    await render(
      <RegraDeCobrancaForm
        enviando={false}
        onSubmit={onSubmit}
        regraExistente={{
          matriculaId: 'matricula-1',
          tipo: 'FixoMensal',
          valor: 300,
          frequenciaSemanalContratada: null,
          baseDeContagemAula: null,
        }}
      />,
    );

    expect(screen.getByDisplayValue('300')).toBeTruthy();
    expect(screen.queryByText('Frequência semanal contratada')).toBeNull();

    await fireEvent.press(screen.getByText('Salvar regra de cobrança'));

    expect(onSubmit).toHaveBeenCalledWith({
      tipo: 'FixoMensal',
      valor: 300,
      frequenciaSemanalContratada: null,
      baseDeContagemAula: null,
    });
  });

  describe('base de contagem de aula (issue #186)', () => {
    it('shows the base de contagem selector for ValorPorAula (default tipo), defaulting to Agendamento', async () => {
      await render(<RegraDeCobrancaForm enviando={false} onSubmit={jest.fn()} />);

      expect(screen.getByText('Como contar as aulas do período?')).toBeTruthy();
      expect(screen.getByText('Todas as aulas agendadas')).toBeTruthy();
      expect(screen.getByText('Só aulas com presença confirmada')).toBeTruthy();
    });

    it('shows the base de contagem selector for FixoPorAula', async () => {
      await render(<RegraDeCobrancaForm enviando={false} onSubmit={jest.fn()} />);

      await fireEvent.press(screen.getByText('Fixo por aula'));

      expect(screen.getByText('Como contar as aulas do período?')).toBeTruthy();
    });

    it('hides the base de contagem selector for FixoMensal', async () => {
      await render(<RegraDeCobrancaForm enviando={false} onSubmit={jest.fn()} />);

      await fireEvent.press(screen.getByText('Fixo mensal'));

      expect(screen.queryByText('Como contar as aulas do período?')).toBeNull();
    });

    it('submits FixoPorAula with the selected baseDeContagemAula', async () => {
      const onSubmit = jest.fn();
      await render(<RegraDeCobrancaForm enviando={false} onSubmit={onSubmit} />);

      await fireEvent.press(screen.getByText('Fixo por aula'));
      await fireEvent.changeText(screen.getByPlaceholderText('50.00'), '50');
      await fireEvent.press(screen.getByText('Só aulas com presença confirmada'));
      await fireEvent.press(screen.getByText('Salvar regra de cobrança'));

      expect(onSubmit).toHaveBeenCalledWith({
        tipo: 'FixoPorAula',
        valor: 50,
        frequenciaSemanalContratada: null,
        baseDeContagemAula: 'PresencaConfirmada',
      });
    });
  });
});
