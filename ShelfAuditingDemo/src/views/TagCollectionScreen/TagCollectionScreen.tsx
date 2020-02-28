import React from 'react'
import { View, Image, Text, TouchableOpacity, FlatList } from 'react-native'
import { NavigationScreenProp, NavigationState, NavigationParams } from 'react-navigation'
import { SearchBar } from 'react-native-elements'
import { styles } from './TagCollectionScreen.style'
import { UnknownProduct, ShelfGap } from '../../../constants'
import { TagItem } from '../../models'

interface TagCollectionScreenProps {
    navigation: NavigationScreenProp<NavigationState, NavigationParams>;
}

interface TagCollectionScreenState {
    filter: string;
    allTags: Array<TagItem>;
    filterTags: Array<TagItem>;
}

export class TagCollectionScreen extends React.Component<TagCollectionScreenProps, TagCollectionScreenState> {
    constructor(props: TagCollectionScreenProps) {
        super(props);
        this.state = {
            filter: '',
            allTags: Array<TagItem>(),
            filterTags: Array<TagItem>()
        };
    }

    componentDidMount() {
        const { navigation, } = this.props;
        let tags = navigation.getParam('tags', Array<TagItem>());
        this.setState({
            allTags: tags, 
            filterTags: tags 
        });
    }

    render() {
        const { filter } = this.state;
        
        return (
            <View style={styles.mainContainer}>
                <SearchBar
                    inputContainerStyle={styles.searchInputContainerStyle}
                    containerStyle={styles.searchContainerStyle}
                    placeholder="Search"
                    onChangeText={this.updateSearchFilter}
                    value={filter}
                />
                
                <FlatList 
                    numColumns={4}
                    data={ this.state.filterTags } 
                    renderItem={({item}) => {

                        const isUnknownProduct = item.name.toLocaleLowerCase() === UnknownProduct.toLocaleLowerCase();
                        const isShelfGap = item.name.toLocaleLowerCase() === ShelfGap.toLocaleLowerCase();

                        return <TouchableOpacity activeOpacity={0.6} style={styles.tagBlockStyle} 
                                    onPress={() => this.selectTag(item)}>
                                    <View>
                                        <View style={[styles.imageContainer, { height: '60%' }]}>
                                            { !isUnknownProduct && !isShelfGap &&
                                                <Image style={styles.imageThumbnail} source={{ uri: item.imageUrl }} />
                                            }
                                            { isUnknownProduct &&
                                                <Image style={styles.imageThumbnail} source={require('../../assets/product.jpg')} />
                                            }
                                            { isShelfGap &&
                                                <Image style={styles.imageThumbnail} source={require('../../assets/gap.jpg')} />
                                            }
                                        </View>
                                        
                                        <View style={[styles.tagContainer, { height: '40%' }]}>
                                            <Text numberOfLines={3} style={styles.tagLabel}>{item.name}</Text>
                                        </View>
                                    </View>
                                </TouchableOpacity>
                    }}
                />

            </View>
        );
    }

    selectTag(tag: TagItem) {
        const { navigation } = this.props;
        if (navigation.state?.params?.returnData) {
            navigation.state.params.returnData(tag);
            navigation.goBack();
        }
    }

    updateSearchFilter = (filter: string) => {
        let allTags = this.state.allTags;
        
        if (!filter) {
            this.setState({ filterTags: allTags });
        } else {
            let filterData = Array<TagItem>();
            allTags.forEach(t => {
                let tagName = t.name.toLocaleLowerCase();
                if (tagName.includes(filter.toLocaleLowerCase())) {
                    filterData.push(t);
                }
            });
            this.setState({ filterTags: filterData });
        }
        this.setState({ filter });
    }
}
