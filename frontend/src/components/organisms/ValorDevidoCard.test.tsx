import { render, screen } from '@testing-library/react-native';

import { ValorDevidoCard } from './ValorDevidoCard';

describe('ValorDevidoCard', () => {
  it('shows the aluno name and the formatted value when a regra is defined', async () => {
    const valorDevido = {
      matriculaId: 'm1',
      alunoUsuarioId: null,
      nome: 'Ana',
      valor: 300,
      semRegraDefinida: false,
    };

    await render(<ValorDevidoCard valorDevido={valorDevido} />);

    expect(screen.getByText('Ana')).toBeTruthy();
    expect(screen.getByText(/300,00/)).toBeTruthy();
  });

  it('shows a "sem regra" message instead of R$ 0,00 when semRegraDefinida is true', async () => {
    const valorDevido = {
      matriculaId: 'm2',
      alunoUsuarioId: null,
      nome: 'Bruno',
      valor: null,
      semRegraDefinida: true,
    };

    await render(<ValorDevidoCard valorDevido={valorDevido} />);

    expect(screen.getByText('Sem regra de cobrança definida')).toBeTruthy();
    expect(screen.queryByText(/R\$\s*0,00/)).toBeNull();
  });
});
