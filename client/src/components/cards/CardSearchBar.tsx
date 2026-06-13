type Props = {
  query: string;
  game: string;
  onQueryChange: (value: string) => void;
  onGameChange: (value: string) => void;
  onSearch: () => void;
};

export function CardSearchBar({ query, game, onQueryChange, onGameChange, onSearch }: Props) {
  return (
    <div className="toolbar">
      <input value={query} onChange={(event) => onQueryChange(event.target.value)} onKeyDown={(event) => event.key === 'Enter' && onSearch()} placeholder="Search cards" />
      <select value={game} onChange={(event) => onGameChange(event.target.value)}>
        <option>All</option>
        <option>Pokemon</option>
        <option>Magic</option>
      </select>
      <button onClick={onSearch}>Search</button>
    </div>
  );
}
