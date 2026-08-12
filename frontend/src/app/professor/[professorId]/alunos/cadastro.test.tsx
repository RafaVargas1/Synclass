import { fireEvent, render, screen, waitFor } from '@testing-library/react-native';

import { cadastrarAlunoProvisorio } from '@/lib/api/alunosProvisorios';

import CadastroAlunoProvisorioScreen from './cadastro';

jest.mock('@/lib/api/alunosProvisorios', () => ({
  cadastrarAlunoProvisorio: jest.fn(),
}));

jest.mock('expo-router', () => ({
  useLocalSearchParams: () => ({ professorId: 'professor-1' }),
}));

const cadastrarAlunoProvisorioMock = cadastrarAlunoProvisorio as jest.Mock;

describe('CadastroAlunoProvisorioScreen', () => {
  beforeEach(() => {
    cadastrarAlunoProvisorioMock.mockReset();
  });

  it('shows an inline confirmation when the Api responds with success', async () => {
    cadastrarAlunoProvisorioMock.mockResolvedValue({
      sucesso: true,
      nome: 'João Pedro',
      identificador: '2024-013',
    });
    await render(<CadastroAlunoProvisorioScreen />);

    await fireEvent.changeText(screen.getByPlaceholderText('Nome do Aluno'), 'João Pedro');
    await fireEvent.changeText(
      screen.getByPlaceholderText('Identificador (ex: número de matrícula)'),
      '2024-013',
    );
    await fireEvent.press(screen.getByText('Cadastrar'));

    await waitFor(() => expect(screen.getByText('Aluno provisório cadastrado!')).toBeTruthy());
    expect(cadastrarAlunoProvisorioMock).toHaveBeenCalledWith({
      professorId: 'professor-1',
      nome: 'João Pedro',
      identificador: '2024-013',
    });
  });

  it('shows the Api error message without crashing when the Api rejects the cadastro', async () => {
    cadastrarAlunoProvisorioMock.mockResolvedValue({
      sucesso: false,
      mensagem: 'Já existe um Aluno provisório com o identificador "2024-013" para este Professor.',
    });
    await render(<CadastroAlunoProvisorioScreen />);

    await fireEvent.changeText(screen.getByPlaceholderText('Nome do Aluno'), 'João Pedro');
    await fireEvent.changeText(
      screen.getByPlaceholderText('Identificador (ex: número de matrícula)'),
      '2024-013',
    );
    await fireEvent.press(screen.getByText('Cadastrar'));

    await waitFor(() =>
      expect(
        screen.getByText('Já existe um Aluno provisório com o identificador "2024-013" para este Professor.'),
      ).toBeTruthy(),
    );
    expect(screen.getByText('Cadastrar')).toBeTruthy();
  });
});
