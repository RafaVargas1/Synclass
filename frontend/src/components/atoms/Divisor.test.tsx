import { render, screen } from '@testing-library/react-native';

import { Divisor } from './Divisor';

describe('Divisor', () => {
  it('renders without a label by default', async () => {
    await render(<Divisor />);

    expect(screen.queryByText(/./)).toBeNull();
  });

  it('renders a centered label when texto is given', async () => {
    await render(<Divisor texto="ou" />);

    expect(screen.getByText('ou')).toBeTruthy();
  });
});
