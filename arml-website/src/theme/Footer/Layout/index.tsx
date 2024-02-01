import React from 'react'
import clsx from 'clsx'
import type { Props } from '@theme/Footer/Layout'
import useBaseUrl from '@docusaurus/useBaseUrl'
import customFields from '@site/src/customFields'
import styles from './styles.module.css'

const FooterContent = () => {
  return <div className={styles.footerContent}></div>
}

export default function FooterLayout({
  style,
  links,
  logo,
  copyright,
}: Props): JSX.Element {
  return (
    <footer
      className={clsx('footer', {
        'footer--dark': style === 'dark',
      })}
    >
      <div className="container container-fluid">
        <div className={styles.footerColumns}>
          <FooterContent />
          {links}
        </div>
        {(logo || copyright) && (
          <div className="footer__bottom text--center">
            {logo && <div className="margin-bottom--sm">{logo}</div>}
            {copyright}
          </div>
        )}
      </div>
    </footer>
  )
}
