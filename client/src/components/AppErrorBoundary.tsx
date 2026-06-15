import { Component, type ErrorInfo, type ReactNode } from 'react';

type Props = {
  children: ReactNode;
  resetKey: string;
};

type State = {
  error: Error | null;
};

export class AppErrorBoundary extends Component<Props, State> {
  state: State = { error: null };

  static getDerivedStateFromError(error: Error): State {
    return { error };
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    console.error('P2W UI render error', error, info);
  }

  componentDidUpdate(previousProps: Props) {
    if (previousProps.resetKey !== this.props.resetKey && this.state.error) {
      this.setState({ error: null });
    }
  }

  render() {
    if (!this.state.error) {
      return this.props.children;
    }

    return (
      <main className="app-error-page">
        <section className="app-error-card">
          <p className="eyebrow">UI error</p>
          <h1>This page hit a render error</h1>
          <p>{this.state.error.message || 'Unknown frontend error'}</p>
          <button onClick={() => this.setState({ error: null })}>Try Again</button>
        </section>
      </main>
    );
  }
}
