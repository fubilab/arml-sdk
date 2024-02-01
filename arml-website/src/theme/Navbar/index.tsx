import React from 'react'
import NavbarLayout from '@theme/Navbar/Layout'
import NavbarContent from '@theme/Navbar/Content'
import Head from '@docusaurus/Head'
import useDocusaurusContext from '@docusaurus/useDocusaurusContext'

export default function Navbar(): JSX.Element {
  const {
    siteConfig: { url },
  } = useDocusaurusContext()
  return (
    <NavbarLayout>
      <Head />
      <NavbarContent />
    </NavbarLayout>
  )
}
