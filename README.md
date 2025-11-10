<a id="readme-top"></a>

<!-- PROJECT LOGO -->
<h1 align="center">
    <a href="https://github.com/acrgardiner/homehq">
        <img width="180" src="images/logo.png">
    </a>
    <br/>
    HomeHQ
</h1>

Track your home assets, receipts, and warranties in one secure place. Built with .NET and Blazor, HomeHQ helps you organize purchased assets and make receipts readily available.

<a href="https://github.com/acrgardiner/homehq">Explore the docs</a> | <a href="https://github.com/acrgardiner/homehq/issues/new?labels=bug&template=bug-report---.md">Report Bug</a> | <a href="https://github.com/acrgardiner/homehq/issues/new?labels=enhancement&template=feature-request---.md">Request Feature</a>

<!-- TABLE OF CONTENTS -->
<details>
  <summary>Table of Contents</summary>
  <ol>
    <li>
      <a href="#about-the-project">About The Project</a>
    </li>
    <li>
      <a href="#getting-started">Getting Started</a>
      <ul>
        <li><a href="#installation">Installation</a></li>
        <li><a href="#installation">Configuration</a></li>
      </ul>
    </li>
    <li><a href="#contributing">Contributing</a></li>
    <li><a href="#license">License</a></li>
  </ol>
</details>



<!-- ABOUT THE PROJECT -->
## ❔ About The Project

HomeHQ is a locally hostable home management solution designed to keep track of household/family information readily accessible in one secure location.

### Key Features

- Asset Management
- Warranty Tracking
- Attachments
- Attributes
- Notes
- Inbuilt image editing for attachments


[![HomeHQ - Dashboard][dashboard-screenshot]]()

[![HomeHQ - Asset][asset-list-screenshot]]()

[![HomeHQ - Asset][asset-detail-screenshot]]()

<p align="right">(<a href="#readme-top">back to top</a>)</p>


<!-- GETTING STARTED -->
## 🚀 Getting Started

This is an example of how you may give instructions on setting up your project locally.
To get a local copy up and running follow these simple example steps.

### Installation

1. Install via docker

**Start the container with `docker run`**

```sh
# Make sure your local config directory exists
docker run -d \
  --name homehq \
  -p 8080:8080 \
  --mount type=bind,source="/path/to/config/dir",target=/www/assets \
  --restart=unless-stopped \
  acrgardiner/homehq:latest
```

**or `docker-compose`**

```yaml
services:
  homehq:
    image: acrgardiner/homehq:latest
    container_name: homehq
    volumes:
      - /path/to/config/dir:/www/assets
    ports:
      - 8080:8080
    restart: unless-stopped
```

| Environment Variable | Description | Default Value |
| :--- | :--- | :--- |
|TRUSTED_PROXIES|Comma separated list of IP's for your trusted proxies||

| Mount Paths | Description |
| :--- | :--- |
|appdata||
|appdata/attachments|Storing full resolution attachment|
|appdata/db|Program database for SQLite|
|appdata/logs|Log files|
|appdata/thumbs|Storing compressed thumbnails of attachments|

<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- CONTRIBUTING -->
## 🤝 Contributing

Contributions are welcome! 

Submit bug reports and feature requests via the GitHub issues page.

Or for developers looking to contribute code, please check and follow our [contribution guidelines](/CONTRIBUTING.md)

<p align="right">(<a href="#readme-top">back to top</a>)</p>

### Top contributors:

<a href="https://github.com/acrgardiner/homehq/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=acrgardiner/homehq" alt="contrib.rocks image" />
</a>



<!-- LICENSE -->
## License

Distributed under the GPL-3.0 license. See `LICENSE.txt` for more information.

<p align="right">(<a href="#readme-top">back to top</a>)</p>


<!-- MARKDOWN LINKS & IMAGES -->
<!-- https://www.markdownguide.org/basic-syntax/#reference-style-links -->
[contributors-shield]: https://img.shields.io/github/contributors/acrgardiner/homehq.svg?style=for-the-badge
[contributors-url]: https://github.com/acrgardiner/homehq/graphs/contributors
[forks-shield]: https://img.shields.io/github/forks/acrgardiner/homehq.svg?style=for-the-badge
[forks-url]: https://github.com/acrgardiner/homehq/network/members
[stars-shield]: https://img.shields.io/github/stars/acrgardiner/homehq.svg?style=for-the-badge
[stars-url]: https://github.com/acrgardiner/homehq/stargazers
[issues-shield]: https://img.shields.io/github/issues/acrgardiner/homehq.svg?style=for-the-badge
[issues-url]: https://github.com/acrgardiner/homehq/issues
[license-shield]: https://img.shields.io/github/license/acrgardiner/homehq.svg?style=for-the-badge
[license-url]: https://github.com/acrgardiner/homehq/blob/master/LICENSE.txt
[dashboard-screenshot]: docs/dashboard.png
[asset-list-screenshot]: docs/asset-list.png
[asset-detail-screenshot]: docs/asset-detail.png