import { fireEvent, render, screen, waitFor } from '@testing-library/react-native';

import { definirRegraDeCobranca } from '@/lib/api/regraDeCobranca';

import RegraDeCobrancaScreen from './regra-de-cobranca';

jest.mock('@/lib/api/regraDeCobranca', () => ({
  definirRegraDeCobranca: jest.fn(),
}));

jest.mock('expo-router', () => ({
  useLocalSearchParams: () => ({ professorId: 'professor-1', matriculaId: 'matricula-1' }),
}));

const definirRegraDeCobrancaMock = definirRegraDeCobranca as jest.Mock;

describe('RegraDeCobrancaScreen', () => {
  beforeEach(() => {
    definirRegraDeCobrancaMock.mockReset();
  });

  it('shows an inline confirmation when the Api responds with success', async () => {
    definirRegraDeCobrancaMock.mockResolvedValue({
      sucesso: true,
      regra: { matriculaId: 'matricula-1', tipo: 'FixoMensal', valor: 300, frequenciaSemanalContratada: null },
    });
    await render(<RegraDeCobrancaScreen />);

    await fireEvent.press(screen.getByText('Fixo mensal'));
    await fireEvent.changeText(screen.getByPlaceholderText('50.00'), '300');
    await fireEvent.press(screen.getByText('Salvar regra de cobrança'));

    await waitFor(() => expect(screen.getByText('Regra de cobrança salva!')).toBeTruthy());
    expect(definirRegraDeCobrancaMock).toHaveBeenCalledWith('professor-1', 'matricula-1', {
      tipo: 'FixoMensal',
      valor: 300,
      frequenciaSemanalContratada: null,
    });
  });

  it('shows the Api error message without crashing when the Api rejects', async () => {
    definirRegraDeCobrancaMock.mockResolvedValue({
      sucesso: false,
      mensagem: 'Matrícula não encontrada: matricula-1.',
    });
    await render(<RegraDeCobrancaScreen />);

    await fireEvent.changeText(screen.getByPlaceholderText('50.00'), '50');
    await fireEvent.changeText(screen.getByPlaceholderText('3'), '3');
    await fireEvent.press(screen.getByText('Salvar regra de cobrança'));

    await waitFor(() =>
      expect(screen.getByText('Matrícula não encontrada: matricula-1.')).toBeTruthy(),
    );
    expect(screen.getByText('Salvar regra de cobrança')).toBeTruthy();
  });
});
