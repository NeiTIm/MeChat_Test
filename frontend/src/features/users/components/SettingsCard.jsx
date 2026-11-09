export function SettingsCard({ title, description, children, danger }) {
  return (
    <section className={`settings-card ${danger ? 'danger-card' : ''}`}>
      <h2>{title}</h2>
      {description && <p className="page-description">{description}</p>}
      {children}
    </section>
  )
}
